using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.SendPortrait)]
public class ClientSendPortraitMessage : ClientMessage
{
    public IReadOnlyList<byte> Portrait { get; set; } = [];

    public string Bio { get; set; } = string.Empty;

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        var totalLength = reader.ReadUInt16();
        if (totalLength <= 0)
        {
            return;
        }

        var portraitLength = reader.ReadUInt16();
        Portrait = reader.ReadBytes(portraitLength);
        Bio = reader.ReadString16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt16((ushort)(Portrait.Count + Bio.Length + 4));
        builder.AppendUInt16((ushort)Portrait.Count);
        builder.AppendBytes(Portrait);
        builder.AppendString16(Bio);
    }
}
