using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SeMoreEvents.ObjectBuilders;
using SeMoreEvents.SessionComponents;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI.Network;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.Sync;
using VRage.Utils;

namespace SeMoreEvents.Components.Events
{
    [MyComponentBuilder(typeof(ObjectBuilderWeather))]
    [MyComponentType(typeof(WeatherEvent))]
    [MyEntityDependencyType(typeof(IMyEventControllerBlock))]
    public class WeatherEvent : MyEventProxyEntityComponent, IMyEventComponentWithGui, IEventControllerEvent
    {
        public override string ComponentTypeDebugString => nameof(WeatherEvent);
        public long UniqueSelectionId => 6844804;
        public MyStringId EventDisplayName => Texts.EventWeatherName;

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected == value)
                    return;
                _isSelected = value;
                
                if (Block == null)
                    return;
                
                if (_isSelected)
                    ((MyCubeGrid)Block.CubeGrid).Schedule(MyCubeGrid.UpdateQueue.BeforeSimulation, Update);
                else
                    ((MyCubeGrid)Block.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, Update);
            }
        }

        private int _tick;
        private void Update()
        {
            if (++_tick < 160) return;
            
            _tick = 0;
            CheckWeather();
        }

        public bool IsThresholdUsed => false;
        public bool IsConditionSelectionUsed => false;
        public bool IsBlocksListUsed => false;

        private static readonly List<MyWeatherEffectDefinition> WeatherEffectDefinitions = new List<MyWeatherEffectDefinition>();
        private static readonly List<MyTerminalControlComboBoxItem> WeatherEffectsContent = new List<MyTerminalControlComboBoxItem>();

        private MySync<long, SyncDirection.BothWays> _selectedWeatherId;
        private long _prevWeatherId;
        private bool _isSelected;

        private IMyEventControllerBlock Block => Entity as IMyEventControllerBlock;

        public WeatherEvent()
        {
            if (WeatherEffectDefinitions.Count == 0)
            {
                WeatherEffectDefinitions.Add(new MyWeatherEffectDefinition
                {
                    Id = new MyDefinitionId(typeof(MyObjectBuilder_WeatherEffect), "None"),
                    DisplayNameEnum = MySpaceTexts.None
                });
                WeatherEffectDefinitions.AddRange(MyDefinitionManager.Static.GetWeatherDefinitions());
                WeatherEffectsContent.AddRange(WeatherEffectDefinitions.Select((t, i) => new MyTerminalControlComboBoxItem
                {
                    Key = i, Value = MyStringId.GetOrCompute(t.Id.SubtypeName)
                }));
            }
        }

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            _selectedWeatherId.Validate = value => value >= 0 && value < WeatherEffectDefinitions.Count;
            _selectedWeatherId.ValueChanged += _ => NotifyValuesChanged();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();
            if (IsSelected)
                ((MyCubeGrid)Block.CubeGrid).DeSchedule(MyCubeGrid.UpdateQueue.BeforeSimulation, Update);
        }
        
        public override MyObjectBuilder_ComponentBase Serialize(bool copy = false)
        {
            var builder = new MyObjectBuilder_ModCustomComponent
            {
                ComponentType = nameof(WeatherEvent),
                CustomModData = _selectedWeatherId.Value.ToString(CultureInfo.InvariantCulture),
                RemoveExistingComponentOnNewInsert = true,
                SubtypeName = nameof(WeatherEvent)
            };
            
            return builder;
        }

        public override void Deserialize(MyObjectBuilder_ComponentBase builder)
        {
            base.Deserialize(builder);
            var customBuilder = (MyObjectBuilder_ModCustomComponent)builder;

            _selectedWeatherId.Value = long.Parse(customBuilder.CustomModData, CultureInfo.InvariantCulture);
        }
        
        public override bool IsSerialized()
        {
            return true;
        }

        public void CreateTerminalInterfaceControls<T>() where T : IMyTerminalBlock
        {
            var comboBox =
                MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, T>("WeatherEvent.WeatherType");
            comboBox.Visible = b => b.Components.Get<WeatherEvent>().IsSelected;
            comboBox.ComboBoxContent = list => list.AddRange(WeatherEffectsContent);
            comboBox.Getter = b => b.Components.Get<WeatherEvent>()._selectedWeatherId;
            comboBox.Setter = (b, value) => b.Components.Get<WeatherEvent>()._selectedWeatherId.Value = value;
            comboBox.Title = MySpaceTexts.Weather;
            
            MyAPIGateway.TerminalControls.AddControl<T>(comboBox);
        }

        public void NotifyValuesChanged()
        {
            if (Block == null || !MyAPIGateway.Multiplayer.IsServer) return;

            var currentWeatherId = GetCurrentWeatherId();
            DetailedInfoSync.SendUpdateDetailedInfo(Block, nameof(WeatherEvent),
                                                    currentWeatherId == _selectedWeatherId ? 1 : 0, 0,
                                                    currentWeatherId);
        }

        private void CheckWeather()
        {
            if (Block == null) return;
            
            var currentWeatherId = GetCurrentWeatherId();

            if (_selectedWeatherId == currentWeatherId)
            {
                Block.TriggerAction(0);
                return;
            }
            else
            {
                Block.TriggerAction(1);
                return;
            }
            _prevWeatherId = currentWeatherId;
        }

        private int GetCurrentWeatherId()
        {
            MyObjectBuilder_WeatherEffect currentWeather;
            MyAPIGateway.Session.WeatherEffects.GetWeather(Block.GetPosition(), out currentWeather);

            if (currentWeather == null)
                return 0;

            var currentSubtypeId = MyStringHash.GetOrCompute(currentWeather.Weather);
            var currentWeatherId = WeatherEffectDefinitions.FindIndex(b => b.Id.SubtypeId == currentSubtypeId);
            return currentWeatherId;
        }

        public bool IsBlockValidForList(IMyTerminalBlock block)
        {
            return false;
        }

        public void AddBlocks(List<IMyTerminalBlock> blocks)
        {
        }

        public void RemoveBlocks(IEnumerable<IMyTerminalBlock> blocks)
        {
        }
        
        public void UpdateDetailedInfo(StringBuilder info, int slot, long entityId, float value)
        {
            info.AppendFormat(MySpaceTexts.EventInfo, EventDisplayName).AppendLine();
            info.AppendFormat(MySpaceTexts.EventBoolBlockInputInfo,
                              MyTexts.GetString(MySpaceTexts.Weather),
                              WeatherEffectDefinitions[(int)value].DisplayNameText).AppendLine();
            info.AppendFormat(MySpaceTexts.EventOutputInfo, slot);
        }
    }
}
