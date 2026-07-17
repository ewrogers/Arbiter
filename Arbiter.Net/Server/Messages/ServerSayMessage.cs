using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Say)]
public class ServerSayMessage : ServerMessage
{
    public PublicMessageType MessageType { get; set; }
    public uint SenderId { get; set; }
    public string Message { get; set; } = string.Empty;

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        MessageType = (PublicMessageType)reader.ReadByte();
        SenderId = reader.ReadUInt32();
        Message = reader.ReadString8();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)MessageType);
        builder.AppendUInt32(SenderId);
        builder.AppendString8(Message);
    }
}
