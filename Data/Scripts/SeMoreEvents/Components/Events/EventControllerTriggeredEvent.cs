using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using SeMoreEvents.ObjectBuilders;
using SeMoreEvents.SessionComponents;
using VRage;
using VRage.Game.Components;
using VRage.Utils;

namespace SeMoreEvents.Components.Events
{
    [MyComponentBuilder(typeof(ObjectBuilderEventControllerTriggered))]
    [MyComponentType(typeof(EventControllerTriggeredEvent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    public class EventControllerTriggeredEvent : MyEventProxyEntityComponent, IMyEventComponentWithGui,
                                                 IEventControllerBooleanEvent
    {
        private IMyEventControllerBlock Block => Entity as IMyEventControllerBlock;

        public override string ComponentTypeDebugString => nameof(EventControllerTriggeredEvent);

        private readonly Dictionary<IMyEventControllerBlock, Action<int>> _blocks =
            new Dictionary<IMyEventControllerBlock, Action<int>>();

        private readonly EventControllerGenericBooleanEvent<IMyEventControllerBlock> _eventGeneric;

        public EventControllerTriggeredEvent()
        {
            _eventGeneric = new EventControllerGenericBooleanEvent<IMyEventControllerBlock>
            {
                EventName = EventDisplayName,
                GetTriggerStateValueString = b => MyTexts.GetString(b == true ? Texts.EventTriggered : Texts.EventNotTriggered),
                GetTriggerStateValue = b => false,
                GetTriggerStateKey = b => (IMyEventControllerBlock)b,
                SubscribeBlockEvent = b =>
                {
                    var block = (IMyEventControllerBlock)b;
                    block.ActionTriggered += _blocks[block] = i => OnActionTriggered(block, i);
                },
                UnsubscribeBlockEvent = b =>
                {
                    var block = (IMyEventControllerBlock)b;
                    Action<int> action;
                    if (!_blocks.TryGetValue(block, out action))
                        return;
                    block.ActionTriggered -= action;
                    _blocks.Remove(block);
                }
            };
        }

        private void OnActionTriggered(IMyEventControllerBlock block, int slot)
        {
            if (Block == null)
                return;
            
            _eventGeneric.RaiseEvent(block, Block, slot > 0);
            DetailedInfoSync.SendUpdateDetailedInfo(Block, nameof(EventControllerTriggeredEvent), 0, 0, slot > 0);
        }

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
        }

        public long UniqueSelectionId => 6844806;
        public MyStringId EventDisplayName => Texts.EventEventControllerTriggeredName;
        public bool IsSelected { get; set; }

        public void NotifyValuesChanged()
        {
            if (Block != null)
                DetailedInfoSync.SendUpdateDetailedInfo(Block, nameof(EventControllerTriggeredEvent), 0, 0, false);
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            return block is IMyEventControllerBlock && Block != block;
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks)
        {
            if (Block != null)
                _eventGeneric.AddBlocks(Block, blocks);
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
            _eventGeneric.RemoveBlocks(blocks);
        }

        public bool IsThresholdUsed => false;
        public bool IsConditionSelectionUsed => false;
        public bool IsBlocksListUsed => true;

        public void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, bool value)
        {
            if (Block != null)
                _eventGeneric.UpdateDetailedInfo(info, 0f, slot, entityId, value);
        }
    }
}