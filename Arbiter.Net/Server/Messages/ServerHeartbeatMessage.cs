using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.RequestCRC)]
public class ServerHeartbeatMessage : ServerMessage
{
    public ushort Request { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Request = reader.ReadUInt16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt16(Request);
    }
}
