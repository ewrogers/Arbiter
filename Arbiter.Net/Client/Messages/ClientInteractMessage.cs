using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.RequestObjectInfo)]
public class ClientInteractMessage : ClientMessage
{
    public InteractionType InteractionType { get; set; }
    public uint? TargetId { get; set; }
    public ushort? TargetX { get; set; }
    public ushort? TargetY { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        InteractionType = (InteractionType)reader.ReadByte();

        switch (InteractionType)
        {
            case InteractionType.Entity:
                TargetId = reader.ReadUInt32();
                break;
            case InteractionType.Tile:
                TargetX = reader.ReadUInt16();
                TargetY = reader.ReadUInt16();
                break;
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)InteractionType);

        switch (InteractionType)
        {
            case InteractionType.Entity:
                builder.AppendUInt32(TargetId ?? 0);
                break;
            case InteractionType.Tile:
                builder.AppendUInt16(TargetX ?? 0);
                builder.AppendUInt16(TargetY ?? 0);
                break;
        }
    }
}
