using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using VRage;
using VRage.ModAPI;
using VRage.Utils;

namespace SeMoreEvents.Components
{
    public class EventControllerGenericEvent<T>
    {
        public MyStringId EventName { get; set; }

        public MyStringId ValueSymbol { get; set; }

        public bool IsObservingBlocks { get; set; }

        public event Action<int, long, float, bool> DetailedInfoChanged;

        public EventControllerGenericEvent()
        {
            ValueSymbol = Texts.PercentSign;
            FormatValue = FormatValueDefault;
            IsObservingBlocks = true;
        }

        private string FormatValueDefault(float value)
        {
            return MyValueFormatter.GetFormatedFloat(value * 100f, 1);
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks, float conditionValue, bool invert)
        {
            foreach (var myTerminalBlock in blocks)
            {
                var t = GetTriggerStateKey(myTerminalBlock);
                if (_observedBlocks.ContainsKey(t)) continue;
				
                _observedBlocks[t] = myTerminalBlock;
                if (MyAPIGateway.Multiplayer.IsServer) SubscribeBlockEvent?.Invoke(myTerminalBlock);
                var num = GetTriggerStateValue(t);
                var myBlockTriggerState = num >= conditionValue ? MyBlockTriggerState.Above : MyBlockTriggerState.Bellow;
                if (invert)
                {
                    myBlockTriggerState = num <= conditionValue ? MyBlockTriggerState.Above : MyBlockTriggerState.Bellow;
                }
                _triggerStates[t] = myBlockTriggerState;
                myTerminalBlock.OnClosing += OnBlockOnClosing;
            }
        }

        private void OnBlockOnClosing(IMyEntity entity)
        {
            IMyTerminalBlock myTerminalBlock;
            if ((myTerminalBlock = entity as IMyTerminalBlock) != null)
            {
                RemoveBlock(myTerminalBlock);
            }
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
            foreach (var myTerminalBlock in blocks)
            {
                RemoveBlock(myTerminalBlock);
            }
        }

        private void RemoveBlock(IMyTerminalBlock block)
        {
            var t = GetTriggerStateKey(block);
            if (t == null)
            {
                return;
            }
            _observedBlocks.Remove(t);
            UnsubscribeBlockEvent.Invoke(block);
            _triggerStates.Remove(t);
            block.OnClosing -= OnBlockOnClosing;
        }

        public void RaiseEvent(T key, IMyEventControllerBlock block, float previousValue, float newValue, float conditionValue)
        {
            var flag = false;
            if (block.IsLowerOrEqualCondition)
            {
                if (previousValue >= conditionValue && newValue <= conditionValue)
                {
                    _lastSlot = AboveActionSlot;
                    TriggerAction(key, block, MyBlockTriggerState.Above, AboveActionSlot);
                    flag = true;
                }
                else if (previousValue <= conditionValue && newValue > conditionValue)
                {
                    _lastSlot = BellowActionSlot;
                    TriggerAction(key, block, MyBlockTriggerState.Bellow, BellowActionSlot);
                    flag = true;
                }
            }
            else if (previousValue >= conditionValue && newValue < conditionValue)
            {
                _lastSlot = BellowActionSlot;
                TriggerAction(key, block, MyBlockTriggerState.Bellow, BellowActionSlot);
                flag = true;
            }
            else if (previousValue <= conditionValue && newValue >= conditionValue)
            {
                _lastSlot = AboveActionSlot;
                TriggerAction(key, block, MyBlockTriggerState.Above, AboveActionSlot);
                flag = true;
            }
            IMyTerminalBlock myTerminalBlock;
            if (_observedBlocks.TryGetValue(key, out myTerminalBlock))
            {
                DetailedInfoChanged?.Invoke(_lastSlot, myTerminalBlock.EntityId, newValue, flag);
                return;
            }
            if (!IsObservingBlocks)
            {
                DetailedInfoChanged?.Invoke(_lastSlot, 0L, newValue, flag);
            }
        }

        public void UpdateDetailedInfo(StringBuilder info, float conditionValue, int slot, long entityId, float value, bool isLowerOrEqualCondition)
        {
            var text = string.Empty;
            if (ValueSymbol != MyStringId.NullOrEmpty)
            {
                text = MyTexts.GetString(ValueSymbol);
            }
            if (EventName != MyStringId.NullOrEmpty)
            {
                var @string = MyTexts.GetString(EventName);
                info.AppendFormat(MySpaceTexts.EventInfo, @string);
                info.AppendLine();
            }

            info.AppendFormat(isLowerOrEqualCondition ? MySpaceTexts.EventBellowEqualInfo : MySpaceTexts.EventAboveEqualInfo);
            info.AppendLine();
            var text2 = FormatValue(conditionValue);
            info.AppendFormat(MySpaceTexts.EventThresholdInfo, text2, text);
            info.AppendLine();
            foreach (var myTerminalBlock in _observedBlocks.Values)
            {
                var num = value;
                if (myTerminalBlock.EntityId == entityId)
                {
                    _clientCache[entityId] = value;
                }
                else if (!_clientCache.TryGetValue(myTerminalBlock.EntityId, out num))
                {
                    var t = GetTriggerStateKey(myTerminalBlock);
                    num = GetTriggerStateValue(t);
                }
                var text3 = FormatValue(num);
                info.AppendFormat(MySpaceTexts.EventBlockInputInfo, myTerminalBlock.CustomName, text3, text);
                info.AppendLine();
            }
            if (!IsObservingBlocks)
            {
                var text4 = FormatValue(value);
                info.AppendFormat(MySpaceTexts.EventInputInfo, text4, text);
                info.AppendLine();
            }
            info.AppendFormat(MySpaceTexts.EventOutputInfo, slot + 1);
        }

        private void TriggerAction(T key, IMyEventControllerBlock block, MyBlockTriggerState triggerState, int actionIndex)
        {
            if (!block.IsWorking)
            {
                return;
            }
            _triggerStates[key] = triggerState;
            if (block.IsAndModeEnabled)
            {
                if (_observedBlocks.Count != _triggerStates.Count) return;
				
                var flag = true;
                using (var enumerator = _triggerStates.Values.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current != triggerState)
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if (flag && triggerState == MyBlockTriggerState.Above)
                {
                    block.TriggerAction(actionIndex);
                    return;
                }
                if (triggerState == MyBlockTriggerState.Bellow)
                {
                    block.TriggerAction(actionIndex);
                }
            }
            else
            {
                block.TriggerAction(actionIndex);
            }
        }

        public void NotifyValuesChanged(IMyEventControllerBlock eventBlock, float conditionValue)
        {
            if (!MyAPIGateway.Multiplayer.IsServer)
                return;
			
            var dictionary = new Dictionary<T, MyBlockTriggerState>();
            foreach (var keyValuePair in _triggerStates)
            {
                var num = GetTriggerStateValue(keyValuePair.Key);
                var value = keyValuePair.Value;
                if (eventBlock.IsLowerOrEqualCondition)
                {
                    if (value == MyBlockTriggerState.Above && num > conditionValue)
                    {
                        dictionary.Add(keyValuePair.Key, MyBlockTriggerState.Bellow);
                    }
                    else if (value == MyBlockTriggerState.Bellow && conditionValue >= num)
                    {
                        dictionary.Add(keyValuePair.Key, MyBlockTriggerState.Above);
                    }
                }
                else if (value == MyBlockTriggerState.Above && conditionValue > num)
                {
                    dictionary.Add(keyValuePair.Key, MyBlockTriggerState.Bellow);
                }
                else if (value == MyBlockTriggerState.Bellow && num >= conditionValue)
                {
                    dictionary.Add(keyValuePair.Key, MyBlockTriggerState.Above);
                }
            }
            foreach (var keyValuePair2 in dictionary)
            {
                _lastSlot = keyValuePair2.Value == MyBlockTriggerState.Bellow ? BellowActionSlot : AboveActionSlot;
                TriggerAction(keyValuePair2.Key, eventBlock, keyValuePair2.Value, _lastSlot);
            }
            DetailedInfoChanged?.Invoke(_lastSlot, 0L, 0f, dictionary.Count != 0);
        }

        private const int AboveActionSlot = 0;
        private const int BellowActionSlot = 1;

        private readonly Dictionary<long, float> _clientCache = new Dictionary<long, float>();

        private readonly Dictionary<T, IMyTerminalBlock> _observedBlocks = new Dictionary<T, IMyTerminalBlock>();

        private readonly Dictionary<T, MyBlockTriggerState> _triggerStates = new Dictionary<T, MyBlockTriggerState>();

        private int _lastSlot;

        public Action<IMyTerminalBlock> SubscribeBlockEvent;

        public Action<IMyTerminalBlock> UnsubscribeBlockEvent;

        public Func<IMyTerminalBlock, T> GetTriggerStateKey;

        public Func<T, float> GetTriggerStateValue;

        public Func<float, string> FormatValue;

        private enum MyBlockTriggerState
        {
            Bellow,
            Above
        }
    }
}