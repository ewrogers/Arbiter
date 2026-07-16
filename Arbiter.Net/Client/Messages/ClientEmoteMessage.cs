using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Emotion)]
public class ClientEmoteMessage : ClientMessage
{
    public Emote Emote { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Emote = (Emote)reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Emote);
    }
}
