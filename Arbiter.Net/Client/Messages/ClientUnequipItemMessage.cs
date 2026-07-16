using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.RemoveEquipment)]
public class ClientUnequipItemMessage : ClientMessage
{
    public EquipmentSlot Slot { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Slot = (EquipmentSlot)reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Slot);
    }
}
