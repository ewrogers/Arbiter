using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.MoveObject)]
public class ServerMoveObjectMessage : ServerMessage
{
    public uint EntityId { get; set; }
    public ushort OriginX { get; set; }
    public ushort OriginY { get; set; }
    public WorldDirection Direction { get; set; }
    public byte Unknown { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        EntityId = reader.ReadUInt32();
        OriginX = reader.ReadUInt16();
        OriginY = reader.ReadUInt16();
        Direction = (WorldDirection)reader.ReadByte();
        Unknown = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt32(EntityId);
        builder.AppendUInt16(OriginX);
        builder.AppendUInt16(OriginY);
        builder.AppendByte((byte)Direction);
        builder.AppendByte(Unknown);
    }
}
