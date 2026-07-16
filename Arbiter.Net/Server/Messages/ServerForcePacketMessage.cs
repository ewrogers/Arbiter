using Arbiter.Net.Annotations;
using Arbiter.Net.Client;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Bounce)]
public class ServerForcePacketMessage : ServerMessage
{
    public ClientCommand ClientCommand { get; set; }
    public IReadOnlyList<byte> Data { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        var length = reader.ReadUInt16();
        ClientCommand = (ClientCommand)reader.ReadByte();
        Data = reader.ReadBytes(length - 1);
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        var dataLength = Math.Min(Data.Count, ushort.MaxValue - 1);
        
        builder.AppendUInt16((ushort)(dataLength + 1));
        builder.AppendByte((byte)Command);
        builder.AppendBytes(Data.Take(dataLength));
    }
}
