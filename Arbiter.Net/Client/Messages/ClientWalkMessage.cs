using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Move)]
public class ClientWalkMessage : ClientMessage
{
    public WorldDirection Direction { get; set; }
    public byte StepCount { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Direction = (WorldDirection)reader.ReadByte();
        StepCount = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Direction);
        builder.AppendByte(StepCount);
    }
}
