using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.FarLook)]
public class ClientFarLookMessage : ClientMessage
{
    public ushort TileX { get; set; }
    public ushort TileY { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        TileX = reader.ReadUInt16();
        TileY = reader.ReadUInt16();
    }
    
    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendUInt16(TileX);
        builder.AppendUInt16(TileY);
    }
}
