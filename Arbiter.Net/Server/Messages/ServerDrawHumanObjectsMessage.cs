using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.DrawHumanObjects)]
public class ServerDrawHumanObjectsMessage : ServerMessage
{
    private static readonly byte[] DefaultMonsterUnknown = new byte[6];
    
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public WorldDirection Direction { get; set; }
    public uint EntityId { get; set; }

    public ushort HeadSprite { get; set; }
    public byte FaceShape { get; set; }
    public DyeColor? HairColor { get; set; }
    public BodySprite? BodySprite { get; set; }
    public SkinColor? SkinColor { get; set; }
    public bool IsTranslucent { get; set; }
    public bool IsHidden { get; set; }

    public ushort? MonsterSprite { get; set; }
    public IReadOnlyList<byte>? MonsterUnknown { get; set; }

    public ushort? ArmsSprite { get; set; }
    public ushort? ArmorSprite { get; set; }
    public DyeColor? PantsColor { get; set; }
    public byte? BootsSprite { get; set; }
    public DyeColor? BootsColor { get; set; }
    public ushort? WeaponSprite { get; set; }
    public byte? ShieldSprite { get; set; }
    public ushort? Accessory1Sprite { get; set; }
    public DyeColor? Accessory1Color { get; set; }
    public ushort? Accessory2Sprite { get; set; }
    public DyeColor? Accessory2Color { get; set; }
    public ushort? Accessory3Sprite { get; set; }
    public DyeColor? Accessory3Color { get; set; }
    public DyeColor? OvercoatColor { get; set; }
    public ushort? OvercoatSprite { get; set; }

    public LanternSize Lantern { get; set; }
    public RestPosition? RestPosition { get; set; }

    public NameTagStyle NameStyle { get; set; }
    public string? Name { get; set; }
    public string? GroupBox { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        X = reader.ReadUInt16();
        Y = reader.ReadUInt16();
        Direction = (WorldDirection)reader.ReadByte();
        EntityId = reader.ReadUInt32();

        var headSprite = reader.ReadUInt16();

        // Displaying as a monster instead
        if (headSprite == 0xFFFF)
        {
            MonsterSprite = SpriteFlags.ClearFlags(reader.ReadUInt16());
            HairColor = (DyeColor)reader.ReadByte();
            BootsColor = (DyeColor)reader.ReadByte();

            // Not sure what these are for
            MonsterUnknown = reader.ReadBytes(6);
        }
        else
        {
            var bodySprite = reader.ReadByte();
            HeadSprite = headSprite;

            var pantsColor = (byte)(bodySprite % 16);

            if (pantsColor != 0)
            {
                bodySprite -= pantsColor;
                PantsColor = (DyeColor)pantsColor;
            }

            BodySprite = (BodySprite)bodySprite;
            ArmsSprite = reader.ReadUInt16();

            BootsSprite = reader.ReadByte();
            ArmorSprite = reader.ReadUInt16();

            ShieldSprite = reader.ReadByte();
            WeaponSprite = reader.ReadUInt16();

            HairColor = (DyeColor)reader.ReadByte();
            BootsColor = (DyeColor)reader.ReadByte();

            Accessory1Color = (DyeColor)reader.ReadByte();
            Accessory1Sprite = reader.ReadUInt16();
            Accessory2Color = (DyeColor)reader.ReadByte();
            Accessory2Sprite = reader.ReadUInt16();
            Accessory3Color = (DyeColor)reader.ReadByte();
            Accessory3Sprite = reader.ReadUInt16();

            Lantern = (LanternSize)reader.ReadByte();
            RestPosition = (RestPosition)reader.ReadByte();

            OvercoatSprite = reader.ReadUInt16();
            OvercoatColor = (DyeColor)reader.ReadByte();

            SkinColor = (SkinColor)reader.ReadByte();
            IsTranslucent = reader.ReadBoolean();

            FaceShape = reader.ReadByte();
        }

        NameStyle = (NameTagStyle)reader.ReadByte();
        Name = reader.ReadString8();
        GroupBox = reader.ReadString8();

        IsHidden = BodySprite == Arbiter.Net.Types.BodySprite.None && !IsTranslucent;
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt16(X);
        builder.AppendUInt16(Y);
        builder.AppendByte((byte)Direction);
        builder.AppendUInt32(EntityId);

        if (MonsterSprite.HasValue)
        {
            builder.AppendUInt16(0xFFFF);   // head sprite
            builder.AppendUInt16(SpriteFlags.SetCreature(MonsterSprite.Value));
            builder.AppendByte((byte)(HairColor ?? DyeColor.Default));
            builder.AppendByte((byte)(BootsColor ?? DyeColor.Default));
            foreach (var b in MonsterUnknown ?? DefaultMonsterUnknown)
            {
                builder.AppendByte(b);
            }
        }
        else
        {
            builder.AppendUInt16(HeadSprite);
            var bodySprite = (byte)(BodySprite ?? Arbiter.Net.Types.BodySprite.None);
            if (PantsColor.HasValue)
            {
                bodySprite += (byte)PantsColor.Value;
            }

            builder.AppendByte(bodySprite);
            builder.AppendUInt16(ArmsSprite ?? 0);
            builder.AppendByte(BootsSprite ?? 0);
            builder.AppendUInt16(ArmorSprite ?? 0);
            builder.AppendByte(ShieldSprite ?? 0);
            builder.AppendUInt16(WeaponSprite ?? 0);
            builder.AppendByte((byte)(HairColor ?? DyeColor.Default));
            builder.AppendByte((byte)(BootsColor ?? DyeColor.Default));
            builder.AppendByte((byte)(Accessory1Color ?? DyeColor.Default));
            builder.AppendUInt16(Accessory1Sprite ?? 0);
            builder.AppendByte((byte)(Accessory2Color ?? DyeColor.Default));
            builder.AppendUInt16(Accessory2Sprite ?? 0);
            builder.AppendByte((byte)(Accessory3Color ?? DyeColor.Default));
            builder.AppendUInt16(Accessory3Sprite ?? 0);
            builder.AppendByte((byte)Lantern);
            builder.AppendByte((byte)(RestPosition ?? Net.Types.RestPosition.None));
            builder.AppendUInt16(OvercoatSprite ?? 0);
            builder.AppendByte((byte)(OvercoatColor ?? DyeColor.Default));
            builder.AppendByte((byte)(SkinColor ?? Net.Types.SkinColor.Default));
            builder.AppendBoolean(IsTranslucent);
            builder.AppendByte(FaceShape);
        }

        builder.AppendByte((byte)NameStyle);
        builder.AppendString8(Name ?? string.Empty);
        builder.AppendString8(GroupBox ?? string.Empty);
    }
}
