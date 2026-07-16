using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.ScreenShot)]
public class ServerShowMapHelpMessage : ServerMessage
{
    public byte MapIndex { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        MapIndex = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte(MapIndex);
    }
}
