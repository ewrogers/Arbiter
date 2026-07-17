using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.VersionCheck)]
public class ServerVersionCheckMessage : ServerMessage
{
    public uint Checksum { get; set; }
    public byte Seed { get; set; }
    public IReadOnlyList<byte> PrivateKey { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        // Not sure what this byte is for
        reader.Skip(1);

        Checksum = reader.ReadUInt32();

        Seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        PrivateKey = reader.ReadBytes(keyLength);
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(0);  // not sure what this byte is for
        
        builder.AppendUInt32(Checksum);
        builder.AppendByte(Seed);
        builder.AppendByte((byte)PrivateKey.Count);
        builder.AppendBytes(PrivateKey);
    }
}
