using System;
using ProtoBuf;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace SeMoreEvents.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class ObjectBuilderTargetAcquired : MyObjectBuilder_ComponentBase
    {
    }

    // maybe later
    [Flags]
    public enum TargetType
    {
        None = 0,
        Station = 2,
        SmallGrid = 4,
        LargeGrid = 8,
        LargeBlock = Station | LargeGrid,
        Grid = SmallGrid | LargeGrid,
        All = None | Station | Grid,
    }
}