using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.AddEquip)]
public class ServerAddEquipMessage : ServerMessage
{
    public EquipmentSlot Slot { get; set; }
    public ushort Sprite { get; set; }
    public DyeColor Color { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Durability { get; set; }
    public uint MaxDurability { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Slot = (EquipmentSlot)reader.ReadByte();
        Sprite = SpriteFlags.ClearFlags(reader.ReadUInt16());
        Color = (DyeColor)reader.ReadByte();
        Name = reader.ReadString8();

        reader.Skip(1); // always zero, not sure what it is

        MaxDurability = reader.ReadUInt32();
        Durability = reader.ReadUInt32();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Slot);
        builder.AppendUInt16(SpriteFlags.SetItem(Sprite));
        builder.AppendByte((byte)Color);
        builder.AppendString8(Name);
        
        builder.AppendByte(0);  // always zero, not sure what it is
        
        builder.AppendUInt32(MaxDurability);
        builder.AppendUInt32(Durability);
    }
}
