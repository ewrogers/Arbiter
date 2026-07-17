using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.ChangeDirection)]
public class ServerChangeDirectionMessage : ServerMessage
{
    public uint EntityId { get; set; }
    public WorldDirection Direction { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        EntityId = reader.ReadUInt32();
        Direction = (WorldDirection)reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt32(EntityId);
        builder.AppendByte((byte)Direction);
    }
}
