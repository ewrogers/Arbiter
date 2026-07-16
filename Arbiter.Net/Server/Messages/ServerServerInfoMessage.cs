using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Browser)]
public class ServerServerInfoMessage : ServerMessage
{
    public ServerInfoType DataType { get; set; }
    public string? Value { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        DataType = (ServerInfoType)reader.ReadByte();
        Value = reader.ReadString8();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)DataType);
        builder.AppendString8(Value ?? string.Empty);
    }
}
