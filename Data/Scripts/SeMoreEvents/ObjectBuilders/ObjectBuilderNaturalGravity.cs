using ProtoBuf;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ObjectBuilders;

namespace SeMoreEvents.ObjectBuilders
{
    // currently keen doesnt support mod object builders in world save, so this class is unused
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class ObjectBuilderNaturalGravity : MyObjectBuilder_ComponentBase
    {
        [ProtoMember(1)]
        public float Gravity { get; set; } = 0.5f;
    }
}