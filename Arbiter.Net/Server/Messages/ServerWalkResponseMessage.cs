using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Move)]
public class ServerWalkResponseMessage : ServerMessage
{
    public WorldDirection Direction { get; set; }
    public ushort PreviousX { get; set; }
    public ushort PreviousY { get; set; }
    public ushort UnknownX { get; set; }
    public ushort UnknownY { get; set; }
    public byte Unknown { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Direction = (WorldDirection)reader.ReadByte();
        PreviousX = reader.ReadUInt16();
        PreviousY = reader.ReadUInt16();

        // not sure what these values are for, visible range?
        UnknownX = reader.ReadUInt16();
        UnknownY = reader.ReadUInt16();
        Unknown = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)Direction);
        builder.AppendUInt16(PreviousX);
        builder.AppendUInt16(PreviousY);
        builder.AppendUInt16(UnknownX);
        builder.AppendUInt16(UnknownY);
        builder.AppendByte(Unknown);
    }
}
