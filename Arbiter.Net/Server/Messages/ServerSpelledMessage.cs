using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Spelled)]
public class ServerSpelledMessage : ServerMessage
{
    public ushort Icon { get; set; }
    public StatusEffectDuration Duration { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Icon = reader.ReadUInt16();
        Duration = (StatusEffectDuration)reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt16(Icon);
        builder.AppendByte((byte)Duration);
    }
}
