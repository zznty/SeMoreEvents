﻿using VRage.Utils;

namespace SeMoreEvents
{
    public static class Texts
    {
        public static readonly MyStringId EventThrustRatioName = MyStringId.GetOrCompute("Event_ThrustRatioName");
        public static readonly MyStringId EventNaturalGravityName = MyStringId.GetOrCompute("Event_NaturalGravityName");
        /// <summary>g</summary>
        public static readonly MyStringId GravitySymbol = MyStringId.GetOrCompute(nameof(GravitySymbol));
        /// <summary>%</summary>
        public static readonly MyStringId PercentSign = MyStringId.GetOrCompute(nameof(PercentSign));
    }
}