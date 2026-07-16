using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.MapSize)]
public class ServerMapInfoMessage : ServerMessage
{
    public ushort MapId { get; set; }
    public MapFlags Flags { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Checksum { get; set; }
    public string Name { get; set; } = string.Empty;

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        MapId = reader.ReadUInt16();

        var widthLo = reader.ReadByte();
        var heightLo = reader.ReadByte();

        Flags = (MapFlags)reader.ReadByte();

        var widthHi = reader.ReadByte();
        var heightHi = reader.ReadByte();

        Width = (ushort)(widthHi << 8 | widthLo);
        Height = (ushort)(heightHi << 8 | heightLo);
        Checksum = reader.ReadUInt16();
        Name = reader.ReadString8();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt16(MapId);
        builder.AppendByte((byte)Width);
        builder.AppendByte((byte)Height);
        builder.AppendByte((byte)Flags);
        builder.AppendByte((byte)(Width >> 8));
        builder.AppendByte((byte)(Height >> 8));
        builder.AppendUInt16(Checksum);
        builder.AppendString8(Name);
    }
}
