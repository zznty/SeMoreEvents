using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SeMoreEvents.ObjectBuilders;
using SeMoreEvents.SessionComponents;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Library.Collections;
using VRage.ModAPI;
using VRage.Utils;

namespace SeMoreEvents.Components.Events
{
    [MyComponentBuilder(typeof(ObjectBuilderProjectionBuilt))]
    [MyComponentType(typeof(ProjectionBuiltEvent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    public class ProjectionBuiltEvent : MyEventProxyEntityComponent, IMyEventComponentWithGui, IEventControllerEvent
    {
        private readonly EventControllerGenericEvent<IMyProjector> _eventGeneric;

        private readonly Dictionary<IMyCubeGrid, HashSet<IMyProjector>> _subscribedProjectors =
            new Dictionary<IMyCubeGrid, HashSet<IMyProjector>>();

        private IMyEventControllerBlock Block => Entity as IMyEventControllerBlock;

        public ProjectionBuiltEvent()
        {
            _eventGeneric = new EventControllerGenericEvent<IMyProjector>
            {
                EventName = EventDisplayName,
                GetTriggerStateKey = b => (IMyProjector)b,
                GetTriggerStateValue = b => (float)(b.TotalBlocks - b.RemainingBlocks) / b.TotalBlocks,
                SubscribeBlockEvent = b =>
                {
                    var projector = (IMyProjector)b;

                    HashSet<IMyProjector> projectorsSet;
                    if (_subscribedProjectors.TryGetValue(projector.CubeGrid, out projectorsSet))
                        projectorsSet.Add(projector);
                    else
                        _subscribedProjectors.Add(projector.CubeGrid, new HashSet<IMyProjector> { projector });

                    projector.CubeGrid.OnBlockIntegrityChanged += GridOnBlockIntegrityChanged;
                    projector.CubeGrid.OnGridSplit += GridOnSplit;
                    projector.CubeGrid.OnBlockAdded += GridOnBlockIntegrityChanged;
                    projector.CubeGrid.OnBlockRemoved += GridOnBlockRemoved;
                },
                UnsubscribeBlockEvent = b =>
                {
                    var projector = (IMyProjector)b;

                    HashSet<IMyProjector> projectorsSet;
                    if (_subscribedProjectors.TryGetValue(projector.CubeGrid, out projectorsSet))
                    {
                        projectorsSet.Remove(projector);
                        if (projectorsSet.Count == 0)
                            _subscribedProjectors.Remove(projector.CubeGrid);
                    }

                    projector.CubeGrid.OnBlockIntegrityChanged -= GridOnBlockIntegrityChanged;
                    projector.CubeGrid.OnGridSplit -= GridOnSplit;
                    projector.CubeGrid.OnBlockAdded -= GridOnBlockIntegrityChanged;
                    projector.CubeGrid.OnBlockRemoved -= GridOnBlockRemoved;
                }
            };
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            _eventGeneric.DetailedInfoChanged += EventGenericOnDetailedInfoChanged;
            MyAPIGateway.Entities.OnEntityRemove += EntitiesOnEntityRemove;
        }

        private void GridOnBlockRemoved(IMySlimBlock slimBlock)
        {
            HashSet<IMyProjector> projectorsSet;
            if (Block == null ||
                !_subscribedProjectors.TryGetValue(slimBlock.CubeGrid, out projectorsSet))
                return;

            foreach (var projector in projectorsSet)
            {
                var projectedBlock =
                    projector.ProjectedGrid.GetCubeBlock(
                        projector.ProjectedGrid.WorldToGridInteger(
                            slimBlock.CubeGrid.GridIntegerToWorld(slimBlock.Position)));

                if (projectedBlock == null)
                    continue;

                _eventGeneric.RaiseEvent(projector, Block,
                                         (float)(projector.TotalBlocks - projector.RemainingBlocks) /
                                         projector.TotalBlocks,
                                         (projector.TotalBlocks - projector.RemainingBlocks - 1f) /
                                         projector.TotalBlocks, Block.Threshold);
            }
        }

        private void GridOnSplit(IMyCubeGrid original, IMyCubeGrid part)
        {
            CheckGridProjectors(original);
            CheckGridProjectors(part);
        }

        private void CheckGridProjectors(IMyCubeGrid original)
        {
            HashSet<IMyProjector> projectorsSet;
            if (Block == null || !_subscribedProjectors.TryGetValue(original, out projectorsSet)) return;

            foreach (var projector in projectorsSet)
            {
                if (projector.ProjectedGrid == null) continue;

                var total = 0f;
                var built = 0f;

                foreach (IMySlimBlock slimBlock in ((MyCubeGrid)projector.ProjectedGrid).CubeBlocks)
                {
                    var projectedBlock =
                        projector.ProjectedGrid.GetCubeBlock(
                            projector.ProjectedGrid.WorldToGridInteger(
                                slimBlock.CubeGrid.GridIntegerToWorld(slimBlock.Position)));
                    
                    if (projectedBlock == null) continue;

                    total++;
                    if (projectedBlock.IsFullIntegrity)
                        built++;
                }
                
                _eventGeneric.RaiseEvent(projector, Block, total == 0 ? 0 : (built - 1) / total,
                                         total == 0 ? 0 : built / total, Block.Threshold);
            }
        }

        private void EntitiesOnEntityRemove(IMyEntity obj)
        {
            MyCubeGrid grid;
            IMyProjector projector;
            if (Block != null && (grid = obj as MyCubeGrid) != null && (projector = grid.Projector) != null &&
                _subscribedProjectors.ContainsKey(projector.CubeGrid))
                _eventGeneric.RaiseEvent(projector, Block,
                                         (float)(projector.TotalBlocks - projector.RemainingBlocks) /
                                         projector.TotalBlocks,
                                         0f, Block.Threshold);
        }

        private void GridOnBlockIntegrityChanged(IMySlimBlock slimBlock)
        {
            HashSet<IMyProjector> projectorsSet;
            if (Block == null || !slimBlock.IsFullIntegrity ||
                !_subscribedProjectors.TryGetValue(slimBlock.CubeGrid, out projectorsSet))
                return;

            foreach (var projector in projectorsSet)
            {
                if (projector.ProjectedGrid == null) continue;

                var projectedBlock =
                    projector.ProjectedGrid.GetCubeBlock(
                        projector.ProjectedGrid.WorldToGridInteger(
                            slimBlock.CubeGrid.GridIntegerToWorld(slimBlock.Position)));

                if (projectedBlock == null)
                    continue;

                _eventGeneric.RaiseEvent(projector, Block,
                                         (float)(projector.TotalBlocks - projector.RemainingBlocks) /
                                         projector.TotalBlocks,
                                         (projector.TotalBlocks - projector.RemainingBlocks + 1f) /
                                         projector.TotalBlocks, Block.Threshold);
            }
        }

        private void EventGenericOnDetailedInfoChanged(int arg1, long arg2, float arg3, bool arg4)
        {
            if (IsSelected)
                DetailedInfoSync.SendUpdateDetailedInfo(Block, nameof(ProjectionBuiltEvent), arg1, arg2, arg3);
        }

        public override string ComponentTypeDebugString => nameof(ProjectionBuiltEvent);

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
        }

        public long UniqueSelectionId => 6844805;
        public MyStringId EventDisplayName => Texts.EventProjectionBuiltName;
        public bool IsSelected { get; set; }

        public void NotifyValuesChanged()
        {
            if (Block != null)
                _eventGeneric.NotifyValuesChanged(Block, Block.Threshold);
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            // make sure that this is not projector table
            IMyProjector projector;
            return (projector = block as IMyProjector) != null &&
                   ((MyProjectorDefinition)((MyCubeBlock)projector).BlockDefinition).AllowWelding;
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks)
        {
            if (Block != null)
                _eventGeneric.AddBlocks(blocks, Block.Threshold, Block.IsLowerOrEqualCondition);
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
            _eventGeneric.RemoveBlocks(blocks);
        }

        public bool IsThresholdUsed => true;
        public bool IsConditionSelectionUsed => true;
        public bool IsBlocksListUsed => true;

        public void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, float value)
        {
            if (Block != null)
                _eventGeneric.UpdateDetailedInfo(info, Block.Threshold, slot, entityId, value,
                                                 Block.IsLowerOrEqualCondition);
        }
    }
}