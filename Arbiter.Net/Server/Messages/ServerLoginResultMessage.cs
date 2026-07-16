using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.LoginCheck)]
public class ServerLoginResultMessage : ServerMessage
{
    public LoginResult Result { get; set; }
    public string? Message { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Result = (LoginResult)reader.ReadByte();
        Message = reader.ReadString8();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)Result);
        builder.AppendString8(Message ?? string.Empty);
    }
}
