using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.UseSpell)]
public class ClientCastSpellMessage : ClientMessage
{
    public byte Slot { get; set; }
    public uint? TargetId { get; set; }
    public ushort? TargetX { get; set; }
    public ushort? TargetY { get; set; }
    public string? TextInput { get; set; }
    public IReadOnlyList<ushort> NumericInputs { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Slot = reader.ReadByte();

        if (reader.IsEndOfPacket())
        {
            return;
        }

        var position = reader.Position;

        // This packet has the spell arguments in the packet
        // Unfortunately, there is no way to know how many or what they are so you have to read all possibilities
        
        if (reader.CanRead(8))
        {
            TargetId = reader.ReadUInt32();
            TargetX = reader.ReadUInt16();
            TargetY = reader.ReadUInt16();
        }
        reader.Position = position;
        
        // Try to read the text input
        TextInput = reader.ReadFixedString(reader.Remaining);
        reader.Position = position;

        // Try to read the numeric inputs
        var numericInputs = new List<ushort>();
        while (!reader.IsEndOfPacket() && reader.CanRead(2))
        {
            numericInputs.Add(reader.ReadUInt16());
        }
        NumericInputs = numericInputs;
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(Slot);
        
        if (TargetId.HasValue && TargetX.HasValue && TargetY.HasValue)
        {
            builder.AppendUInt32(TargetId.Value);
            builder.AppendUInt16(TargetX.Value);
            builder.AppendByte((byte)TargetY.Value);
        }
        else if (!string.IsNullOrEmpty(TextInput))
        {
            builder.AppendNullTerminatedString(TextInput);
        }
        else if (NumericInputs.Count > 0)
        {
            foreach (var value in NumericInputs)
            {
                builder.AppendUInt16(value);
            }
        }
        else
        {
            builder.AppendByte(0x00);
        }
    }
}
