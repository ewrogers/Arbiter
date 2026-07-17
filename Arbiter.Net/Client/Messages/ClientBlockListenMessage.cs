using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.BlockListen)]
public class ClientBlockListenMessage : ClientMessage
{
    public IgnoreUserAction Action { get; set; }
    
    public string? Name { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Action = (IgnoreUserAction)reader.ReadByte();

        if (Action is IgnoreUserAction.AddUser or IgnoreUserAction.RemoveUser)
        {
            Name = reader.ReadString8();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)Action);

        if (Action is IgnoreUserAction.AddUser or IgnoreUserAction.RemoveUser)
        {
            builder.AppendString8(Name!);
        }
    }
}
