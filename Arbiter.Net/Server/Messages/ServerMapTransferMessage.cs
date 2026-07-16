using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.MapPart)]
public class ServerMapTransferMessage : ServerMessage
{
    public ushort RowY { get; set; }
    public IReadOnlyList<byte> Data { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        RowY = reader.ReadUInt16();
        Data = reader.ReadToEnd();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt16(RowY);
        builder.AppendBytes(Data);
    }
}
