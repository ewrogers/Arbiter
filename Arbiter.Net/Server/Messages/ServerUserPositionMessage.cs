using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.UserPosition)]
public class ServerUserPositionMessage : ServerMessage
{
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public ushort UnknownX { get; set; }
    public ushort UnknownY { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
        UnknownX = reader.ReadUInt16();
        UnknownY = reader.ReadUInt16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt16(X);
        builder.AppendUInt16(Y);
        builder.AppendUInt16(UnknownX);
        builder.AppendUInt16(UnknownY);
    }
}
