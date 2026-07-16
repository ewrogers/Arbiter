using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.BlockInput)]
public class ServerShowSpinnerMessage : ServerMessage
{
    public bool IsVisible { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        IsVisible = !reader.ReadBoolean();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendBoolean(!IsVisible);
    }
}
