using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.ChangeHour)]
public class ServerLightLevelMessage : ServerMessage
{
    public byte TimeOfDay { get; set; }
    public byte Lighting { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        TimeOfDay = reader.ReadByte();
        Lighting = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(TimeOfDay);
        builder.AppendByte(Lighting);
    }
}
