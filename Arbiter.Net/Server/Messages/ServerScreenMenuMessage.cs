using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.ScreenMenu)]
public class ServerScreenMenuMessage : ServerMessage
{
    public DialogMenuType MenuType { get; set; }
    public EntityTypeFlags EntityType { get; set; }
    public uint? EntityId { get; set; }
    public ushort? Sprite { get; set; }
    public SpriteType SpriteType { get; set; }
    public byte? Color { get; set; }
    public ushort? PursuitId { get; set; }
    public bool ShowGraphic { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }
    public string? Prompt { get; set; }
    public List<ServerDialogMenuChoice> MenuChoices { get; set; } = [];
    public List<ServerItemMenuChoice> ItemChoices { get; set; } = [];
    public List<byte> InventorySlots { get; set; } = [];
    public List<ServerSpellMenuChoice> SpellChoices { get; set; } = [];
    public List<ServerSkillMenuChoice> SkillChoices { get; set; } = [];
    public byte? Unknown1 { get; set; }
    public byte? Unknown2 { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        MenuType = (DialogMenuType)reader.ReadByte();

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
        ShowGraphic = !reader.ReadBoolean(); // inverted for some reason

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

        if (MenuType is DialogMenuType.Menu or DialogMenuType.MenuWithArgs)
        {
            Prompt = MenuType == DialogMenuType.MenuWithArgs ? reader.ReadString8() : null;

            var choiceCount = reader.ReadByte();
            for (var i = 0; i < choiceCount; i++)
            {
                MenuChoices.Add(new ServerDialogMenuChoice
                {
                    Text = reader.ReadString8(),
                    PursuitId = reader.ReadUInt16()
                });
            }
        }
        else if (MenuType is DialogMenuType.TextInput or DialogMenuType.TextInputWithArgs)
        {
            Prompt = MenuType == DialogMenuType.TextInputWithArgs ? reader.ReadString8() : null;
            PursuitId = reader.ReadUInt16();
        }
        else if (MenuType is DialogMenuType.ItemChoices)
        {
            PursuitId = reader.ReadUInt16();
            var itemCount = reader.ReadUInt16();

            for (var i = 0; i < itemCount; i++)
            {
                ItemChoices.Add(new ServerItemMenuChoice
                {
                    Sprite = SpriteFlags.ClearFlags(reader.ReadUInt16()),
                    Color = (DyeColor)reader.ReadByte(),
                    Price = reader.ReadUInt32(),
                    Name = reader.ReadString8(),
                    Description = reader.ReadString8()
                });
            }
        }
        else if (MenuType is DialogMenuType.UserInventory)
        {
            PursuitId = reader.ReadUInt16();
            var slotCount = reader.ReadByte();

            for (var i = 0; i < slotCount; i++)
            {
                InventorySlots.Add(reader.ReadByte());
            }
        }
        else if (MenuType is DialogMenuType.SpellChoices)
        {
            PursuitId = reader.ReadUInt16();
            var spellCount = reader.ReadUInt16();

            for (var i = 0; i < spellCount; i++)
            {
                var spriteType = (SpriteType)reader.ReadByte();

                SpellChoices.Add(new ServerSpellMenuChoice
                {
                    Sprite = SpriteFlags.ClearFlags(reader.ReadUInt16()),
                    Color = (DyeColor)reader.ReadByte(),
                    Name = reader.ReadString8()
                });
            }
        }
        else if (MenuType is DialogMenuType.SkillChoices)
        {
            PursuitId = reader.ReadUInt16();
            var skillCount = reader.ReadUInt16();

            for (var i = 0; i < skillCount; i++)
            {
                var spriteType = (SpriteType)reader.ReadByte();

                SkillChoices.Add(new ServerSkillMenuChoice
                {
                    Sprite = SpriteFlags.ClearFlags(reader.ReadUInt16()),
                    Color = (DyeColor)reader.ReadByte(),
                    Name = reader.ReadString8()
                });
            }
        }
        else if (MenuType is DialogMenuType.UserSkills or DialogMenuType.UserSpells)
        {
            PursuitId = reader.ReadUInt16();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)MenuType);

        builder.AppendByte((byte)EntityType);
        builder.AppendUInt32(EntityId ?? 0);
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

        builder.AppendUInt16(spriteWithFlags);
        builder.AppendByte(Color ?? 0);
        builder.AppendBoolean(!ShowGraphic);

        builder.AppendString8(Name ?? string.Empty);
        builder.AppendString16(Content ?? string.Empty);

        if (MenuType is DialogMenuType.Menu or DialogMenuType.MenuWithArgs)
        {
            if (MenuType == DialogMenuType.MenuWithArgs)
            {
                builder.AppendString8(Prompt ?? string.Empty);
            }

            builder.AppendByte((byte)MenuChoices.Count);
            foreach (var choice in MenuChoices)
            {
                builder.AppendString8(choice.Text);
                builder.AppendUInt16(choice.PursuitId);
            }
        }
        else if (MenuType is DialogMenuType.TextInput or DialogMenuType.TextInputWithArgs)
        {
            if (MenuType == DialogMenuType.TextInputWithArgs)
            {
                builder.AppendString8(Prompt ?? string.Empty);
            }

            builder.AppendUInt16(PursuitId ?? 0);
        }
        else if (MenuType is DialogMenuType.ItemChoices)
        {
            builder.AppendUInt16(PursuitId ?? 0);
            builder.AppendUInt16((ushort)ItemChoices.Count);

            foreach (var item in ItemChoices)
            {
                builder.AppendUInt16(SpriteFlags.SetItem(item.Sprite));
                builder.AppendByte((byte)item.Color);
                builder.AppendUInt32(item.Price);
                builder.AppendString8(item.Name);
                builder.AppendString8(item.Description);
            }
        }
        else if (MenuType is DialogMenuType.UserInventory)
        {
            builder.AppendUInt16(PursuitId ?? 0);
            builder.AppendByte((byte)InventorySlots.Count);

            foreach (var slot in InventorySlots)
            {
                builder.AppendByte(slot);
            }
        }
        else if (MenuType is DialogMenuType.SpellChoices)
        {
            builder.AppendUInt16(PursuitId ?? 0);
            builder.AppendUInt16((ushort)SpellChoices.Count);

            foreach (var spell in SpellChoices)
            {
                builder.AppendByte((byte)SpriteType.Spell);
                builder.AppendUInt16(spell.Sprite);
                builder.AppendByte((byte)spell.Color);
                builder.AppendString8(spell.Name);
            }
        }
        else if (MenuType is DialogMenuType.SkillChoices)
        {
            builder.AppendUInt16(PursuitId ?? 0);
            builder.AppendUInt16((ushort)SkillChoices.Count);

            foreach (var skill in SkillChoices)
            {
                builder.AppendByte((byte)SpriteType.Skill);
                builder.AppendUInt16(skill.Sprite);
                builder.AppendByte((byte)skill.Color);
                builder.AppendString8(skill.Name);
            }
        }
        else if (MenuType is DialogMenuType.UserSkills or DialogMenuType.UserSpells)
        {
            builder.AppendUInt16(PursuitId ?? 0);
        }
    }
}
