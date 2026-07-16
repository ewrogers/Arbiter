using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Use)]
public class ClientUseItemMessage : ClientMessage
{
    public byte Slot { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Slot = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(Slot);
    }
}
