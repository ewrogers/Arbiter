using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Give)]
public class ClientGiveMessage : ClientMessage
{
    public byte Slot { get; set; }
    public uint EntityId { get; set; }
    public byte Quantity { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Slot = reader.ReadByte();
        EntityId = reader.ReadUInt32();
        Quantity = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(Slot);
        builder.AppendUInt32(EntityId);
        builder.AppendByte(Quantity);
    }
}
