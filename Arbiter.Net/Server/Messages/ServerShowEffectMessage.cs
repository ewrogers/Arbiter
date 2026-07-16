using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.EffectLayer)]
public class ServerShowEffectMessage : ServerMessage
{
    public uint TargetId { get; set; }
    public ushort TargetAnimation { get; set; }
    
    public ushort? TargetX { get; set; }
    public ushort? TargetY { get; set; }
    
    public uint? SourceId { get; set; }
    public ushort? SourceAnimation { get; set; }
    
    public ushort AnimationDuration { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        TargetId = reader.ReadUInt32();

        if (TargetId == 0)
        {
            TargetAnimation = reader.ReadUInt16();
            AnimationDuration = reader.ReadUInt16();
            TargetX = reader.ReadUInt16();
            TargetY = reader.ReadUInt16();
        }
        else
        {
            SourceId = reader.ReadUInt32();
            TargetAnimation = reader.ReadUInt16();
            SourceAnimation = reader.ReadUInt16();
            AnimationDuration = reader.ReadUInt16();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt32(TargetId);

        if (TargetId == 0)
        {
            builder.AppendUInt16(TargetAnimation);
            builder.AppendUInt16(AnimationDuration);
            builder.AppendUInt16(TargetX ?? 0);
            builder.AppendUInt16(TargetY ?? 0);
        }
        else
        {
            builder.AppendUInt32(SourceId ?? 0);
            builder.AppendUInt16(TargetAnimation);
            builder.AppendUInt16(SourceAnimation ?? 0);
            builder.AppendUInt16(AnimationDuration);
        }
    }
}
