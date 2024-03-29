﻿using VRage.Utils;

namespace SeMoreEvents
{
    public static class Texts
    {
        public static readonly MyStringId EventThrustRatioName = MyStringId.GetOrCompute("Event_ThrustRatioName");
        public static readonly MyStringId EventNaturalGravityName = MyStringId.GetOrCompute("Event_NaturalGravityName");
        public static readonly MyStringId EventTargetAcquiredName = MyStringId.GetOrCompute("Event_TargetAcquiredName");
        public static readonly MyStringId EventWeatherName = MyStringId.GetOrCompute("Event_WeatherName");

        public static readonly MyStringId EventProjectionBuiltName = MyStringId.GetOrCompute("Event_ProjectionBuiltName");

        public static readonly MyStringId EventEventControllerTriggeredName = MyStringId.GetOrCompute("Event_EventControllerTriggeredName");

        /// <summary>g</summary>
        public static readonly MyStringId GravitySymbol = MyStringId.GetOrCompute(nameof(GravitySymbol));

        /// <summary>%</summary>
        public static readonly MyStringId PercentSign = MyStringId.GetOrCompute(nameof(PercentSign));

        /// <summary>m</summary>
        public static readonly MyStringId LengthUnitSymbol = MyStringId.GetOrCompute(nameof(LengthUnitSymbol));

        /// <summary>
        /// triggered
        /// </summary>
        public static readonly MyStringId EventTriggered = MyStringId.GetOrCompute(nameof(EventTriggered));
        
        /// <summary>
        /// not triggered
        /// </summary>
        public static readonly MyStringId EventNotTriggered = MyStringId.GetOrCompute(nameof(EventNotTriggered));
    }
}