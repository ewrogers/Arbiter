using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.ActionDelay)]
public class ServerActionDelayMessage : ServerMessage
{
    public AbilityType AbilityType { get; set; }
    public byte Slot { get; set; }
    public uint Seconds { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        AbilityType = (AbilityType)reader.ReadByte();
        Slot = reader.ReadByte();
        Seconds = reader.ReadUInt32();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)AbilityType);
        builder.AppendByte(Slot);
        builder.AppendUInt32(Seconds);
    }
}
