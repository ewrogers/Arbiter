using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Quit)]
public class ClientRequestExitMessage : ClientMessage
{
    public ClientExitReason? Reason { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        if (reader.CanRead(1))
        {
            Reason = (ClientExitReason)reader.ReadByte();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        if (Reason.HasValue)
        {
            builder.AppendByte((byte)Reason.Value);
        }
    }
}
