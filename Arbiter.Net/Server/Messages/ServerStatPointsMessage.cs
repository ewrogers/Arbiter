using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.StatPoints)]
public class ServerStatPointsMessage : ServerMessage
{
    public bool FlashButtons { get; set; }
    public byte StatPoints { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        FlashButtons = reader.ReadBoolean();
        StatPoints = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendBoolean(FlashButtons);
        builder.AppendByte(StatPoints);
    }
}
