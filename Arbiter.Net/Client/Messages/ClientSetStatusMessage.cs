using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.UserChangeState)]
public class ClientSetStatusMessage : ClientMessage
{
    public SocialStatus Status { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Status = (SocialStatus)reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Status);
    }
}
