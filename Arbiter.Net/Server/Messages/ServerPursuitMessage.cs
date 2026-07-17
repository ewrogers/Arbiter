using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.PursuitMessage)]
public class ServerPursuitMessage : ServerMessage
{
    public DialogType DialogType { get; set; }
    public EntityTypeFlags EntityType { get; set; }
    public uint? EntityId { get; set; }
    public ushort? Sprite { get; set; }
    public SpriteType SpriteType { get; set; }
    public byte? Color { get; set; }
    public ushort? PursuitId { get; set; }
    public ushort? StepId { get; set; }
    public bool HasPreviousButton { get; set; }
    public bool HasNextButton { get; set; }
    public bool ShowGraphic { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }
    public List<string> MenuChoices { get; set; } = [];
    public string? InputPrompt { get; set; }
    public byte? InputMaxLength { get; set; }
    public string? InputDescription { get; set; }
    public byte? Unknown1 { get; set; }
    public byte? Unknown2 { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        DialogType = (DialogType)reader.ReadByte();

        if (DialogType == DialogType.CloseDialog)
        {
            return;
        }

        EntityType = (EntityTypeFlags)reader.ReadByte();
        EntityId = reader.ReadUInt32();
        Unknown1 = reader.ReadByte();

        var spritePrimary = reader.ReadUInt16();
        SpriteType = SpriteFlags.GetSpriteType(spritePrimary);
        
        Sprite = SpriteFlags.ClearFlags(spritePrimary);
        Color = reader.ReadByte();
        Unknown2 = reader.ReadByte();

        var spriteSecondary = reader.ReadUInt16();
        var colorSecondary = reader.ReadByte();

        PursuitId = reader.ReadUInt16();
        StepId = reader.ReadUInt16();

        HasPreviousButton = reader.ReadBoolean();
        HasNextButton = reader.ReadBoolean();
        ShowGraphic = !reader.ReadBoolean();    // inverted for some reason

        Name = reader.ReadString8();
        Content = reader.ReadString16();

        if (Sprite == 0)
        {
            Sprite = SpriteFlags.ClearFlags(spriteSecondary);
            SpriteType = SpriteFlags.GetSpriteType(spriteSecondary);
        }

        if (Color == 0)
        {
            Color = colorSecondary;
        }

        if (DialogType is DialogType.Menu or DialogType.CreatureMenu)
        {
            var choiceCount = reader.ReadByte();
            for (var i = 0; i < choiceCount; i++)
            {
                MenuChoices.Add(reader.ReadString8());
            }
        }
        else if (DialogType == DialogType.TextInput)
        {
            InputPrompt = reader.ReadString8();
            InputMaxLength = reader.ReadByte();
            InputDescription = reader.ReadString8();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)DialogType);

        if (DialogType == DialogType.CloseDialog)
        {
            builder.AppendByte(0);
            return;
        }

        builder.AppendByte((byte)EntityType);
        builder.AppendUInt32(EntityId!.Value);
        builder.AppendByte(Unknown1 ?? 0x1);

        var spriteWithFlags = SpriteType switch
        {
            SpriteType.Monster => SpriteFlags.SetCreature(Sprite ?? 0),
            SpriteType.Item => SpriteFlags.SetItem(Sprite ?? 0),
            _ => Sprite ?? 0
        };
            
        builder.AppendUInt16(spriteWithFlags);
        builder.AppendByte(Color ?? 0);
        builder.AppendByte(Unknown2 ?? 0x1);

        builder.AppendUInt16(spriteWithFlags); // spriteSecondary
        builder.AppendByte(Color ?? 0); // colorSecondary

        builder.AppendUInt16(PursuitId!.Value);
        builder.AppendUInt16(StepId ?? 0);

        builder.AppendBoolean(HasPreviousButton);
        builder.AppendBoolean(HasNextButton);
        builder.AppendBoolean(!ShowGraphic); // inverted for some reason

        builder.AppendString8(Name ?? string.Empty);
        builder.AppendString16(Content ?? string.Empty);

        if (DialogType is DialogType.Menu or DialogType.CreatureMenu)
        {
            builder.AppendByte((byte)MenuChoices.Count);
            foreach (var choice in MenuChoices)
            {
                builder.AppendString8(choice);
            }
        }
        else if (DialogType == DialogType.TextInput)
        {
            builder.AppendString8(InputPrompt ?? string.Empty);
            builder.AppendByte(InputMaxLength ?? 0xFF);
            builder.AppendString8(InputDescription ?? string.Empty);
        }
    }
}
