using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.EnterEditingMode)]
public class ServerEnterEditingModeMessage : ServerMessage
{
    public byte Slot { get; set; }
    public NotepadStyle Style { get; set; }
    public byte Height { get; set; }
    public byte Width { get; set; }
    public string Content { get; set; } = string.Empty;

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Slot = reader.ReadByte();
        Style = (NotepadStyle)reader.ReadByte();
        Height = reader.ReadByte();
        Width = reader.ReadByte();
        Content = reader.ReadString16();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte(Slot);
        builder.AppendByte((byte)Style);
        builder.AppendByte(Height);
        builder.AppendByte(Width);
        builder.AppendString16(Content);
    }
}
