using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.FieldMap)]
public class ClientFieldMapMessage : ClientMessage
{
    public ushort MapId { get; set; }
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public ushort Checksum { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Checksum = reader.ReadUInt16();
        MapId = reader.ReadUInt16();
        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt16(Checksum);
        builder.AppendUInt16(MapId);
        builder.AppendUInt16(X);
        builder.AppendUInt16(Y);
    }
}
