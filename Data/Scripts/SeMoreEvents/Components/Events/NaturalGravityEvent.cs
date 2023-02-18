using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sandbox.Game.Entities;
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
    [MyComponentBuilder(typeof(ObjectBuilderNaturalGravity))]
    [MyComponentType(typeof(NaturalGravityEvent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    public class NaturalGravityEvent : MyEventProxyEntityComponent, IMyEventComponentWithGui, IEventControllerEvent
    {
        public override string ComponentTypeDebugString => nameof(NaturalGravityEvent);
        public long UniqueSelectionId => 6844802;
        public MyStringId EventDisplayName => Texts.EventNaturalGravityName;
        public bool IsThresholdUsed => false;
        public bool IsConditionSelectionUsed => true;
        public bool IsBlocksListUsed => false;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value) return;
                
                _isSelected = value;

                if (value)
                    Block.CubeGrid.PositionComp.OnPositionChanged += Update;
                else
                    Block.CubeGrid.PositionComp.OnPositionChanged -= Update;
            }
        }

        private MySync<float, SyncDirection.BothWays> _gravity;
        
        private IMyEventControllerBlock Block => Entity as IMyEventControllerBlock;

        private readonly EventControllerGenericEvent<IMyCubeGrid> _eventGeneric;
        private float _prevGravity;
        private bool _isSelected;

        public NaturalGravityEvent()
        {
            _eventGeneric = new EventControllerGenericEvent<IMyCubeGrid>
            {
                EventName = EventDisplayName,
                GetTriggerStateKey = b => b.CubeGrid,
                GetTriggerStateValue = GetGravity,
                FormatValue = value => value.ToString("F2"),
                ValueSymbol = Texts.GravitySymbol,
                SubscribeBlockEvent = b => b.CubeGrid.PositionComp.OnPositionChanged += Update,
                UnsubscribeBlockEvent = b => b.CubeGrid.PositionComp.OnPositionChanged -= Update,
                IsObservingBlocks = false
            };
            _eventGeneric.DetailedInfoChanged += EventGenericOnDetailedInfoChanged;
        }

        private static float GetGravity(IMyEntity b)
        {
            float naturalGravityInterference;
            var gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(b.GetPosition(), out naturalGravityInterference);
            return MathHelper.Clamp(gravity.Length() / 9.81f, -1f, 1f);
        }

        private void EventGenericOnDetailedInfoChanged(int arg1, long arg2, float arg3, bool arg4)
        {
            DetailedInfoSync.SendUpdateDetailedInfo(Block, nameof(NaturalGravityEvent), arg1, arg2, arg3);
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var customBuilder = (MyObjectBuilder_ModCustomComponent)builder;
            _gravity.Value = float.Parse(customBuilder.CustomModData);
        }

        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var builder = new MyObjectBuilder_ModCustomComponent
            {
                ComponentType = nameof(NaturalGravityEvent),
                CustomModData = _gravity.Value.ToString(CultureInfo.InvariantCulture),
                RemoveExistingComponentOnNewInsert = true,
                SubtypeName = nameof(NaturalGravityEvent)
            };
            
            return builder;
        }
        
        public override bool IsSerialized()
        {
            return true;
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            _gravity.ValidateRange(-1f, 1f);
            _gravity.ValueChanged += _ => NotifyValuesChanged();
        }

        public override void SetContainer(MyComponentContainer container)
        {
            base.SetContainer(container);
            if (IsSelected && Block != null)
            {
                Block.CubeGrid.PositionComp.OnPositionChanged += Update;
                Update(null);
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            Block.CubeGrid.PositionComp.OnPositionChanged -= Update;
        }

        private void Update(MyPositionComponentBase myPositionComponentBase)
        {
            var gravity = GetGravity(Block.CubeGrid);
            
            if (Math.Abs(gravity - _prevGravity) < .01)
                return;
            
            _eventGeneric.RaiseEvent(Block.CubeGrid, Block, _prevGravity, gravity, _gravity);
            _prevGravity = gravity;
        }

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
            var slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, T>("NaturalGravityEvent.Gravity");
            slider.Writer = (b, sb) => sb.AppendFormat("{0:F}g", b.Components.Get<NaturalGravityEvent>()._gravity.Value);
            slider.Visible = b => b.Components.Get<NaturalGravityEvent>().IsSelected;
            slider.Getter = b => b.Components.Get<NaturalGravityEvent>()._gravity;
            slider.Setter = (b, value) => b.Components.Get<NaturalGravityEvent>()._gravity.Value = value;
            slider.Title = MySpaceTexts.EventControllerBlock_Threshold_Title;
            slider.Tooltip = MySpaceTexts.EventControllerBlock_Threshold_Tooltip;
            slider.SetLimits(-1f, 1f);
            
            MyAPIGateway.TerminalControls.AddControl<T>(slider);
        }

        public void NotifyValuesChanged()
        {
            if (Block != null)
                _eventGeneric.NotifyValuesChanged(Block, _gravity);
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            return false;
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks)
        {
            _eventGeneric.AddBlocks(blocks, _gravity, Block.IsLowerOrEqualCondition);
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
            _eventGeneric.RemoveBlocks(blocks);
        }

        public void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, float value)
        {
            _eventGeneric.UpdateDetailedInfo(info, _gravity, slot, entityId, value, Block.IsLowerOrEqualCondition);
        }
    }
}