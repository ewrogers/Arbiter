using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Metadata)]
public class ClientMetadataMessage : ClientMessage
{
    public MetadataRequestType RequestType { get; set; }
    public string? Name { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        RequestType = (MetadataRequestType)reader.ReadByte();

        if (RequestType == MetadataRequestType.GetMetadata)
        {
            Name = reader.ReadString8();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)RequestType);
        
        if (RequestType == MetadataRequestType.GetMetadata)
        {
            builder.AppendString8(Name!);
        }
    }
}
