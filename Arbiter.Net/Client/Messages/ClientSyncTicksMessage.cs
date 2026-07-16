using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.CheckTime)]
public class ClientSyncTicksMessage : ClientMessage
{
    public uint ServerTickCount { get; set; }
    public uint ClientTickCount { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        ServerTickCount = reader.ReadUInt32();
        ClientTickCount = reader.ReadUInt32();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt32(ServerTickCount);
        builder.AppendUInt32(ClientTickCount);
    }
}
