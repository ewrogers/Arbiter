using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Quit)]
public class ServerExitResponseMessage : ServerMessage
{
    public byte Result { get; set; }
    public ushort Unknown { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Result = reader.ReadByte();
        Unknown = reader.ReadUInt16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(Result);
        builder.AppendUInt16(Unknown);
    }
}
