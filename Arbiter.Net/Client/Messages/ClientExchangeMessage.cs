using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Exchange)]
public class ClientExchangeMessage : ClientMessage
{
    public ExchangeClientActionType Action { get; set; }
    public uint TargetId { get; set; }
    public byte? Slot { get; set; }
    public byte? Quantity { get; set; }
    public uint? GoldAmount { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Action = (ExchangeClientActionType)reader.ReadByte();
        TargetId = reader.ReadUInt32();

        switch (Action)
        {
            case ExchangeClientActionType.AddItem or ExchangeClientActionType.AddStackableItem:
                Slot = reader.ReadByte();
                Quantity = Action == ExchangeClientActionType.AddStackableItem ? reader.ReadByte() : (byte)1;
                break;
            case ExchangeClientActionType.SetGold:
                GoldAmount = reader.ReadUInt32();
                break;
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
    
        builder.AppendByte((byte)Action);
        builder.AppendUInt32(TargetId);
    
        switch (Action)
        {
            case ExchangeClientActionType.AddItem or ExchangeClientActionType.AddStackableItem:
                builder.AppendByte(Slot ?? 0);
                builder.AppendByte(Quantity ?? 1);
                break;
            case ExchangeClientActionType.SetGold:
                builder.AppendUInt32(GoldAmount ?? 0);
                break;
        }
    }
}
