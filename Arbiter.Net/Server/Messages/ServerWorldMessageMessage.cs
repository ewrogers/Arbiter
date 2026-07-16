using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Message)]
public class ServerWorldMessageMessage : ServerMessage
{
    public WorldMessageType MessageType { get; set; }
    public string Message { get; set; } = string.Empty;

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        MessageType = (WorldMessageType)reader.ReadByte();
        Message = reader.ReadString16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)MessageType);
        builder.AppendString16(Message);
    }
}
