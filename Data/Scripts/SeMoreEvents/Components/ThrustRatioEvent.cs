using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using SeMoreEvents.ObjectBuilders;
using VRage.Game.Components;
using VRage.Utils;

namespace SeMoreEvents.Components
{
    [MyComponentBuilder(typeof(ObjectBuilderThrustRatio))]
    [MyComponentType(typeof(ThrustRatioEvent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    public class ThrustRatioEvent : MyEventProxyEntityComponent, IMyEventComponentWithGui
    {
        public override string ComponentTypeDebugString => nameof(ThrustRatioEvent);
        public long UniqueSelectionId => 6844801;
        public MyStringId EventDisplayName => Texts.EventThrustRatioName;
        public bool IsThresholdUsed => true;
        public bool IsConditionSelectionUsed => true;
        public bool IsBlocksListUsed => true;
        public bool IsSelected { get; set; }
        
        private IMyEventControllerBlock Block => Entity as IMyEventControllerBlock;

        private readonly Dictionary<IMyThrust, ThrustState> _subscriptions = new Dictionary<IMyThrust, ThrustState>();
        private readonly EventControllerGenericEvent<IMyThrust> _eventGeneric;

        public ThrustRatioEvent()
        {
            _eventGeneric = new EventControllerGenericEvent<IMyThrust>
            {
                EventName = EventDisplayName,
                GetTriggerStateKey = b => b as IMyThrust,
                GetTriggerStateValue = b => b.CurrentThrust / b.MaxThrust,
                SubscribeBlockEvent = b => _subscriptions[(IMyThrust)b] = new ThrustState(),
                UnsubscribeBlockEvent = b => _subscriptions.Remove((IMyThrust)b),
            };
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            ((MyCubeGrid)Block.CubeGrid).Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, Update, 7);
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            ((MyCubeGrid)Block.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, Update);
        }

        private void Update()
        {
            if (_subscriptions.Count == 0)
                return;

            foreach (var pair in _subscriptions)
            {
                var currentThrust = pair.Key.CurrentThrust;
                
                if (Math.Abs(currentThrust - pair.Value.PreviousThrust) < 10f)
                    continue;

                var previousThrust = pair.Value.PreviousThrust;
                pair.Value.PreviousThrust = currentThrust;
                
                if (Block != null)
                    _eventGeneric.RaiseEvent(pair.Key, Block, previousThrust / pair.Key.MaxThrust, currentThrust / pair.Key.MaxThrust, Block.Threshold);
            }
        }

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
        }
        
        public void NotifyValuesChanged()
        {
            if (Block != null)
                _eventGeneric.NotifyValuesChanged(Block, Block.Threshold);
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            return block is IMyThrust;
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
    }
}