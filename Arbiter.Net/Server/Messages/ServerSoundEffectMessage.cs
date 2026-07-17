using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.SoundEffect)]
public class ServerSoundEffectMessage : ServerMessage
{
    public byte Sound { get; set; }
    public byte? Track { get; set; }
    public ushort? Unknown { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Sound = reader.ReadByte();
        
        if (Sound == 0xFF)
        {
            Track = reader.ReadByte();
            Unknown = reader.ReadUInt16();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte(Sound);

        if (Sound != 0xFF)
        {
            return;
        }

        builder.AppendByte(Track ?? 0);
        builder.AppendUInt16(Unknown ?? 0);
    }
}
