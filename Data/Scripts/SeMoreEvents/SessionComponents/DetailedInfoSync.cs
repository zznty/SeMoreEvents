using ProtoBuf;
using Sandbox.ModAPI;
using SeMoreEvents.Components;
using VRage.Game.Components;
using VRage.ModAPI;

namespace SeMoreEvents.SessionComponents
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DetailedInfoSync : MySessionComponentBase
    {
        private const ushort Id = 46613;

        public override void BeforeStart()
        {
            base.BeforeStart();
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Id, MessageHandler);
        }

        private static void MessageHandler(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            if (!fromServer)
                return;

            var message = MyAPIGateway.Utilities.SerializeFromBinary<EventChangeMessageBase>(data);

            IMyEntity entity;
            IMyTerminalBlock block;
            if (!MyAPIGateway.Entities.TryGetEntityById(message.BlockId, out entity) || (block = entity as IMyTerminalBlock) == null)
                return;
            
            foreach (var componentType in block.Components.GetComponentTypes())
            {
                if (componentType.Name == message.EventType)
                {
                    MyComponentBase componentBase;
                    if (!block.Components.TryGet(componentType, out componentBase))
                        continue;
                    
                    block.ClearDetailedInfo();
                    var info = block.GetDetailedInfo();
                    
                    if (message is EventChangeMessageBoolean)
                        ((IEventControllerBooleanEvent)componentBase).UpdateDetailedInfo(info, message.Slot, message.EntityId, ((EventChangeMessageBoolean)message).Value);
                    else if (message is EventChangeMessage)
                        ((IEventControllerEvent)componentBase).UpdateDetailedInfo(info, message.Slot, message.EntityId, ((EventChangeMessage)message).Value);
                    
                    block.SetDetailedInfoDirty();
                }
            }
        }

        public static void SendUpdateDetailedInfo(IMyTerminalBlock block, string componentType, int slot, long entityId, bool value)
        {
            var message = new EventChangeMessageBoolean
            {
                BlockId = block.EntityId,
                EventType = componentType,
                EntityId = entityId,
                Slot = slot,
                Value = value
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(message);
            
            MessageHandler(Id, data, MyAPIGateway.Multiplayer.MyId, true);
            MyAPIGateway.Multiplayer.SendMessageToOthers(Id, data);
        }
        
        public static void SendUpdateDetailedInfo(IMyTerminalBlock block, string componentType, int slot, long entityId, float value)
        {
            var message = new EventChangeMessage
            {
                BlockId = block.EntityId,
                EventType = componentType,
                EntityId = entityId,
                Slot = slot,
                Value = value
            };

            var data = MyAPIGateway.Utilities.SerializeToBinary(message);
            
            MessageHandler(Id, data, MyAPIGateway.Multiplayer.MyId, true);
            MyAPIGateway.Multiplayer.SendMessageToOthers(Id, data);
        }
    }

    [ProtoContract]
    [ProtoInclude(10, typeof(EventChangeMessageBoolean))]
    [ProtoInclude(20, typeof(EventChangeMessage))]
    public abstract class EventChangeMessageBase
    {
        [ProtoMember(1)]
        public long BlockId { get; set; }
        [ProtoMember(2)]
        public string EventType { get; set; }
        [ProtoMember(3)]
        public long EntityId { get; set; }
        [ProtoMember(4)]
        public int Slot { get; set; }
    }

    [ProtoContract]
    public class EventChangeMessageBoolean : EventChangeMessageBase
    {
        [ProtoMember(1)]
        public bool Value { get; set; }
    }
    
    [ProtoContract]
    public class EventChangeMessage : EventChangeMessageBase
    {
        [ProtoMember(1)]
        public float Value { get; set; }
    }
}