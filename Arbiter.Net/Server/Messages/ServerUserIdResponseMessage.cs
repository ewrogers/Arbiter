using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.UserId)]
public class ServerUserIdResponseMessage : ServerMessage
{
    public uint UserId { get; set; }
    
    public byte Nonce { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        UserId = reader.ReadUInt32();
        Nonce = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt32(UserId);
        builder.AppendByte(Nonce);
    }
}
