using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.TransferServer)]
public class ClientAuthenticateMessage : ClientMessage
{
    public byte Seed { get; set; }
    public IReadOnlyList<byte> PrivateKey { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public uint ConnectionId { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        PrivateKey = reader.ReadBytes(keyLength);
        Name = reader.ReadString8();
        ConnectionId = reader.ReadUInt32();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(Seed);
        builder.AppendByte((byte)PrivateKey.Count);
        if (PrivateKey.Count > 0)
        {
            builder.AppendBytes(PrivateKey);
        }
        
        builder.AppendString8(Name);
        builder.AppendUInt32(ConnectionId);
    }
}
