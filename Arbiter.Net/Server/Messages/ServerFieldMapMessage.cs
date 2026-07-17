using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.FieldMap)]
public class ServerFieldMapMessage : ServerMessage
{
    public string FieldName { get; set; } = string.Empty;
    public byte FieldIndex { get; set; }
    
    public List<ServerWorldMapNode> Locations { get; set; } = [];
    
    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        FieldName = reader.ReadString8();

        var nodeCount = reader.ReadByte();
        
        FieldIndex = reader.ReadByte();

        for (var i = 0; i < nodeCount; i++)
        {
            var node = new ServerWorldMapNode
            {
                ScreenX = reader.ReadUInt16(),
                ScreenY = reader.ReadUInt16(),
                Name = reader.ReadString8(),
                Checksum = reader.ReadUInt16(),
                MapId = reader.ReadUInt16(),
                MapX = reader.ReadUInt16(),
                MapY = reader.ReadUInt16()
            };
            
            Locations.Add(node);
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendString8(FieldName);
        builder.AppendByte((byte)Locations.Count);
        builder.AppendByte(FieldIndex);

        foreach (var node in Locations)
        {
            builder.AppendUInt16(node.ScreenX);
            builder.AppendUInt16(node.ScreenY);
            builder.AppendString8(node.Name);
            builder.AppendUInt16(node.Checksum);
            builder.AppendUInt16(node.MapId);
            builder.AppendUInt16(node.MapX);
            builder.AppendUInt16(node.MapY);
        }
    }
}
