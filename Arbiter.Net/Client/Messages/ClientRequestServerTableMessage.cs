using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.MultiServer)]
public class ClientRequestServerTableMessage : ClientMessage
{
    public bool NeedsServerTable { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        NeedsServerTable = reader.ReadBoolean();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendBoolean(NeedsServerTable);
    }
}
