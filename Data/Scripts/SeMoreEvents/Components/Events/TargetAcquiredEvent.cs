using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SeMoreEvents.ObjectBuilders;
using SeMoreEvents.SessionComponents;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Sync;
using VRage.Utils;
using VRageMath;

namespace SeMoreEvents.Components.Events
{
    [MyComponentBuilder(typeof(ObjectBuilderTargetAcquired))]
    [MyComponentType(typeof(TargetAcquiredEvent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    public class TargetAcquiredEvent : MyEventProxyEntityComponent, IMyEventComponentWithGui, IEventControllerEvent
    {
        public override string ComponentTypeDebugString => nameof(TargetAcquiredEvent);
        public long UniqueSelectionId => 6844803;
        public MyStringId EventDisplayName => Texts.EventTargetAcquiredName;
        public bool IsSelected { get; set; }
        public bool IsThresholdUsed => false;
        public bool IsConditionSelectionUsed => true;
        public bool IsBlocksListUsed => true;

        private MySync<float, SyncDirection.BothWays> _distance;

        private readonly EventControllerGenericEvent<TargetWatcher>
            _eventGeneric;

        private readonly Dictionary<IMyTerminalBlock, TargetWatcher> _targetWatchers =
            new Dictionary<IMyTerminalBlock, TargetWatcher>();

        private IMyEventControllerBlock Block => Entity as IMyEventControllerBlock;

        public TargetAcquiredEvent()
        {
            _eventGeneric = new EventControllerGenericEvent<TargetWatcher>
            {
                EventName = EventDisplayName,
                ValueSymbol = Texts.LengthUnitSymbol,
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                FormatValue = value => value == float.MaxValue ? MyTexts.GetString(MySpaceTexts.None) : value.ToString("N1"),
                GetTriggerStateKey = GetOrCreateWatcher,
                GetTriggerStateValue = b => b.GetDistance(),
                SubscribeBlockEvent = b => GetOrCreateWatcher(b).Subscribe(),
                UnsubscribeBlockEvent = b => GetOrCreateWatcher(b).UnSubscribe()
            };
            if (MyAPIGateway.Multiplayer.IsServer)
                _eventGeneric.DetailedInfoChanged += EventGenericOnDetailedInfoChanged;
        }

        private TargetWatcher GetOrCreateWatcher(IMyTerminalBlock b)
        {
            TargetWatcher watcher;
            if (_targetWatchers.TryGetValue(b, out watcher))
                return watcher;

            return _targetWatchers[b] = new TargetWatcher(Block, this, GetComponent(b), b);
        }

        private void EventGenericOnDetailedInfoChanged(int arg1, long arg2, float arg3, bool arg4)
        {
            if (IsSelected)
                DetailedInfoSync.SendUpdateDetailedInfo(Block, nameof(TargetAcquiredEvent), arg1, arg2, arg3);
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            _distance.ValidateRange(0, 2500);
            _distance.ValueChanged += _ => NotifyValuesChanged();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            foreach (var watcher in _targetWatchers.Values)
            {
                watcher.UnSubscribe();
            }
            _targetWatchers.Clear();
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var builder = new MyObjectBuilder_ModCustomComponent
            {
                ComponentType = nameof(TargetAcquiredEvent),
                CustomModData = _distance.Value.ToString(CultureInfo.InvariantCulture),
                RemoveExistingComponentOnNewInsert = true,
                SubtypeName = nameof(TargetAcquiredEvent)
            };
            
            return builder;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var customBuilder = (MyObjectBuilder_ModCustomComponent)builder;

            _distance.Value = float.Parse(customBuilder.CustomModData, CultureInfo.InvariantCulture);
        }
        
        public override bool IsSerialized()
        {
            return true;
        }

        private static IMySearchEnemyComponent GetComponent(IMyTerminalBlock block)
        {
            // because component container has class generic restriction

            IMyDefensiveCombatBlock defensiveCombatBlock;
            if ((defensiveCombatBlock = block as IMyDefensiveCombatBlock) != null)
                return defensiveCombatBlock.SearchEnemyComponent;

            IMyOffensiveCombatBlock offensiveCombatBlock;
            if ((offensiveCombatBlock = block as IMyOffensiveCombatBlock) != null)
                return offensiveCombatBlock.SearchEnemyComponent;

            return null;
        }

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
            var slider =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>(
                    "TargetAcquiredEvent.ThresholdDistance");
            slider.Writer = (b, sb) => sb.Append(b.Components.Get<TargetAcquiredEvent>()._distance.Value.ToString("N1"))
                                         .AppendFormat(Texts.LengthUnitSymbol);
            slider.Visible = b => b.Components.Get<TargetAcquiredEvent>().IsSelected;
            slider.Getter = b => b.Components.Get<TargetAcquiredEvent>()._distance;
            slider.Setter = (b, value) => b.Components.Get<TargetAcquiredEvent>()._distance.Value = value;
            slider.Title = MySpaceTexts.EventControllerBlock_Threshold_Title;
            slider.Tooltip = MySpaceTexts.EventControllerBlock_Threshold_Tooltip;
            slider.SetLimits(0, 2500);

            MyAPIGateway.TerminalControls.AddControl<T>(slider);
        }

        public void NotifyValuesChanged()
        {
            if (Block != null)
                _eventGeneric.NotifyValuesChanged(Block, _distance);
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            return GetComponent(block) != null;
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks)
        {
            if (Block != null)
                _eventGeneric.AddBlocks(blocks, _distance, Block.IsLowerOrEqualCondition);
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
            _eventGeneric.RemoveBlocks(blocks);
        }

        public void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, float value)
        {
            _eventGeneric.UpdateDetailedInfo(info, _distance, slot, entityId, value, Block.IsLowerOrEqualCondition);
        }

        private class TargetWatcher
        {
            private readonly IMyEventControllerBlock _block;
            private readonly TargetAcquiredEvent _component;
            private readonly IMySearchEnemyComponent _searchEnemyComponent;
            private readonly IMyTerminalBlock _searchOwner;
            private IMyEntity _currentTarget;

            private readonly Dictionary<long, float> _previousDistances = new Dictionary<long, float>();

            public TargetWatcher(IMyEventControllerBlock block, TargetAcquiredEvent component,
                                 IMySearchEnemyComponent searchEnemyComponent, IMyTerminalBlock searchOwner)
            {
                _block = block;
                _component = component;
                _searchEnemyComponent = searchEnemyComponent;
                _searchOwner = searchOwner;
            }

            public void Subscribe()
            {
                _searchEnemyComponent.TargetChanged += SearchEnemyComponentOnTargetChanged;
                _searchEnemyComponent.SearchComplete += SearchEnemyComponentOnSearchComplete;
                if (_searchEnemyComponent.FoundEnemy != null)
                    SubscribeToTarget(GetTopMostParentOrGrid(_searchEnemyComponent.FoundEnemy));
            }
            
            public void UnSubscribe()
            {
                _searchEnemyComponent.TargetChanged -= SearchEnemyComponentOnTargetChanged;
                _searchEnemyComponent.SearchComplete -= SearchEnemyComponentOnSearchComplete;
                if (_searchEnemyComponent.FoundEnemy != null)
                    UnSubscribeFromTarget(GetTopMostParentOrGrid(_searchEnemyComponent.FoundEnemy));
            }

            private void SearchEnemyComponentOnSearchComplete(IMyEntity obj)
            {
                if (obj != null)
                    TargetOnPositionChanged(GetTopMostParentOrGrid(obj).PositionComp);
            }

            private void SearchEnemyComponentOnTargetChanged(IMyEntity oldTarget, IMyEntity newTarget, bool fromSearch)
            {
                if (oldTarget != null) UnSubscribeFromTarget(GetTopMostParentOrGrid(oldTarget));
                if (newTarget != null) SubscribeToTarget(GetTopMostParentOrGrid(newTarget));
            }

            private IMyEntity GetTopMostParentOrGrid(IMyEntity entity)
            {
                IMyCubeBlock block;
                return (block = entity as IMyCubeBlock) != null ? block.CubeGrid : entity.GetTopMostParent();
            }

            private void SubscribeToTarget(IMyEntity target)
            {
                target.PositionComp.OnPositionChanged -= TargetOnPositionChanged;
                target.OnClosing += TargetOnClosing;
                _currentTarget = target;
                
                TargetOnPositionChanged(target.PositionComp);
            }
            
            private void UnSubscribeFromTarget(IMyEntity target)
            {
                target.PositionComp.OnPositionChanged -= TargetOnPositionChanged;
                target.OnClosing += TargetOnClosing;
                _currentTarget = null;
                
                float previousDistance;
                float currentDistance;
                GetDistancesToTarget(target, out previousDistance, out currentDistance);
                
                _component._eventGeneric.RaiseEvent(this, _block, previousDistance, float.MaxValue, _component._distance);
            }

            private void TargetOnClosing(IMyEntity target)
            {
                UnSubscribeFromTarget(target);
            }

            public float GetDistance()
            {
                if (_currentTarget == null)
                    return float.MaxValue;
                
                float previousDistance;
                float currentDistance;
                GetDistancesToTarget(_currentTarget, out previousDistance, out currentDistance);

                return currentDistance;
            }

            private void GetDistancesToTarget(IMyEntity target, out float previous, out float current)
            {
                if (!_previousDistances.TryGetValue(target.EntityId, out previous))
                    previous = float.MaxValue;
                
                current = Vector3.Distance(target.GetPosition(), _searchOwner.GetPosition());
                _previousDistances[target.EntityId] = current;
            }

            private void TargetOnPositionChanged(MyPositionComponentBase positionComp)
            {
                float previousDistance;
                float currentDistance;
                GetDistancesToTarget(positionComp.Entity, out previousDistance, out currentDistance);
                
                _component._eventGeneric.RaiseEvent(this, _block, previousDistance, currentDistance, _component._distance);
            }
        }
    }
}