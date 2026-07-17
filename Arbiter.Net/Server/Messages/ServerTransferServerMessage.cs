using System.Net;
using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.TransferServer)]
public class ServerTransferServerMessage : ServerMessage
{
    public IPAddress Address { get; set; } = IPAddress.None;
    public ushort Port { get; set; }
    public byte Seed { get; set; }
    public IReadOnlyList<byte> PrivateKey { get; set; } = [];
    public string Name { get; set; } = string.Empty;
    public uint ConnectionId { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Address = reader.ReadIPv4Address();
        Port = reader.ReadUInt16();

        reader.Skip(1); // remaining count

        Seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        PrivateKey = reader.ReadBytes(keyLength);

        Name = reader.ReadString8();
        ConnectionId = reader.ReadUInt32();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendIPv4Address(Address);
        builder.AppendUInt16(Port);

        // The seven extra bytes are for the seed, key length, name length, and connection ID (u32)
        var remainingCount = Name.Length + PrivateKey.Count + 7;
        builder.AppendByte((byte)remainingCount);

        builder.AppendByte(Seed);
        builder.AppendByte((byte)PrivateKey.Count);
        builder.AppendBytes(PrivateKey);

        builder.AppendString8(Name);
        builder.AppendUInt32(ConnectionId);
    }
}
