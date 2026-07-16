using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.AddInventory)]
public class ServerAddItemMessage : ServerMessage
{
    public byte Slot { get; set; }
    public ushort Sprite { get; set; }
    public DyeColor Color { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Quantity { get; set; }
    public bool IsStackable { get; set; }
    public uint Durability { get; set; }
    public uint MaxDurability { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        Slot = reader.ReadByte();
        Sprite = SpriteFlags.ClearFlags(reader.ReadUInt16());
        Color = (DyeColor)reader.ReadByte();
        Name = reader.ReadString8();
        Quantity = reader.ReadUInt32();
        IsStackable = reader.ReadBoolean();
        MaxDurability = reader.ReadUInt32();
        Durability = reader.ReadUInt32();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        var spriteWithFlags = SpriteFlags.SetItem(Sprite);
        
        builder.AppendByte(Slot);
        builder.AppendUInt16(spriteWithFlags);
        builder.AppendByte((byte)Color);
        builder.AppendString8(Name);
        builder.AppendUInt32(Quantity);
        builder.AppendBoolean(IsStackable);
        builder.AppendUInt32(MaxDurability);
        builder.AppendUInt32(Durability);
    }
}
