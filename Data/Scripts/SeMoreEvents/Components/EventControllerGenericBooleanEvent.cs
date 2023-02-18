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
    public class EventControllerGenericBooleanEvent<T>
    {
		public MyStringId EventName { get; set; }

		public event Action<int, long, bool?, bool> DetailedInfoChanged;

		public void AddBlocks(IMyEntity parentEntity, List<IMyTerminalBlock> blocks)
		{
			foreach (var myTerminalBlock in blocks)
			{
				var t = GetTriggerStateKey(myTerminalBlock);
				if (!_observedBlocks.ContainsKey(t))
				{
					_observedBlocks[t] = myTerminalBlock;
					if (MyAPIGateway.Multiplayer.IsServer)
					{
						SubscribeBlockEvent?.Invoke(myTerminalBlock);
					}
					if (parentEntity is IMyEventControllerBlock)
					{
						var flag = GetTriggerStateValue(t);
						_triggerStates[t] = flag;
					}
					myTerminalBlock.OnClosing += OnBlockOnClosing;
				}
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
			UnsubscribeBlockEvent?.Invoke(block);
			_triggerStates.Remove(t);
			block.OnClosing -= OnBlockOnClosing;
		}

		public void RaiseEvent(T key, IMyEventControllerBlock block, bool? newValue)
		{
			var flag = false;
			if (newValue != null)
			{
				_lastSlot = (newValue.Value ? TrueActionSlot : FalseActionSlot);
				TriggerAction(key, block, newValue.Value, _lastSlot);
				flag = true;
			}
			IMyTerminalBlock myTerminalBlock;
			if (_observedBlocks.TryGetValue(key, out myTerminalBlock))
			{
				DetailedInfoChanged?.Invoke(_lastSlot, myTerminalBlock.EntityId, newValue, flag);
			}
		}

		public void UpdateDetailedInfo(StringBuilder info, float conditionValue, int slot, long entityId, bool? value)
		{
			if (EventName != MyStringId.NullOrEmpty)
			{
				var @string = MyTexts.GetString(EventName);
				info.AppendFormat(MySpaceTexts.EventInfo, @string);
				info.AppendLine();
			}
			foreach (var myTerminalBlock in _observedBlocks.Values)
			{
				var flag = value;
				if (myTerminalBlock.EntityId == entityId)
				{
					_clientCache[entityId] = value;
				}
				else if (!_clientCache.TryGetValue(myTerminalBlock.EntityId, out flag))
				{
					var t = GetTriggerStateKey(myTerminalBlock);
					flag = GetTriggerStateValue(t);
				}
				var text = GetTriggerStateValueString(flag);
				info.AppendFormat(MySpaceTexts.EventBoolBlockInputInfo, myTerminalBlock.CustomName, text);
				info.AppendLine();
			}
			info.AppendFormat(MySpaceTexts.EventOutputInfo, slot + 1);
		}

		private void TriggerAction(T key, IMyEventControllerBlock block, bool triggerState, int actionIndex)
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
				foreach (var flag2 in _triggerStates.Values)
				{
					if ((flag2.GetValueOrDefault() == triggerState) & (flag2 != null)) continue;
						
					flag = false;
					break;
				}
				if (flag && triggerState)
				{
					block.TriggerAction(actionIndex);
					return;
				}
				if (!triggerState)
				{
					block.TriggerAction(actionIndex);
				}
			}
			else
			{
				block.TriggerAction(actionIndex);
			}
		}

		public void NotifyValuesChanged(IMyEventControllerBlock eventBlock)
		{
			var dictionary = new Dictionary<T, bool>();
			foreach (var keyValuePair in _triggerStates)
			{
				var flag = GetTriggerStateValue(keyValuePair.Key);
				var value = keyValuePair.Value;
				if (!((value.GetValueOrDefault() == flag.GetValueOrDefault()) & (value != null == (flag != null))) && flag != null)
				{
					dictionary.Add(keyValuePair.Key, flag.Value);
				}
			}
			foreach (var keyValuePair2 in dictionary)
			{
				TriggerAction(keyValuePair2.Key, eventBlock, keyValuePair2.Value, keyValuePair2.Value ? TrueActionSlot : FalseActionSlot);
			}
			DetailedInfoChanged?.Invoke(_lastSlot, 0L, null, dictionary.Count != 0);
		}

		private const int TrueActionSlot = 0;

		private const int FalseActionSlot = 1;

		private readonly Dictionary<long, bool?> _clientCache = new Dictionary<long, bool?>();

		private readonly Dictionary<T, IMyTerminalBlock> _observedBlocks = new Dictionary<T, IMyTerminalBlock>();

		private readonly Dictionary<T, bool?> _triggerStates = new Dictionary<T, bool?>();

		private int _lastSlot;

		public Action<IMyTerminalBlock> SubscribeBlockEvent;

		public Action<IMyTerminalBlock> UnsubscribeBlockEvent;

		public Func<IMyTerminalBlock, T> GetTriggerStateKey;

		public Func<T, bool?> GetTriggerStateValue;

		public Func<bool?, string> GetTriggerStateValueString;
	}
}