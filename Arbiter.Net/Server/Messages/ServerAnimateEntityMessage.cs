using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Motion)]
public class ServerAnimateEntityMessage : ServerMessage
{
    public uint EntityId { get; set; }
    public BodyAnimation Animation { get; set; }
    public ushort Duration { get; set; }
    public byte Sound { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        EntityId = reader.ReadUInt32();
        Animation = (BodyAnimation)reader.ReadByte();
        Duration = reader.ReadUInt16();
        Sound = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt32(EntityId);
        builder.AppendByte((byte)Animation);
        builder.AppendUInt16(Duration);
        builder.AppendByte(Sound);
    }
}
