using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.NewUserAppearance)]
public class ClientNewUserAppearanceMessage : ClientMessage
{
    public byte HairStyle { get; set; }
    public GenderFlags Gender { get; set; }
    public DyeColor HairColor { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);    
        
        HairStyle = reader.ReadByte();
        Gender = (GenderFlags)reader.ReadByte();
        HairColor = (DyeColor)reader.ReadByte();
    }
    
    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte(HairStyle);
        builder.AppendByte((byte)Gender);
        builder.AppendByte((byte)HairColor);
    }
}
