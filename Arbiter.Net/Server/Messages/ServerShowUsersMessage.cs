using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.ShowUsers)]
public class ServerShowUsersMessage : ServerMessage
{
    public ushort WorldCount { get; set; }
    public ushort CountryCount { get; set; }
    public List<ServerWorldListUser> Users { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        WorldCount = reader.ReadUInt16();
        CountryCount = reader.ReadUInt16();

        for (var i = 0; i < CountryCount; i++)
        {
            var classWithFlags = reader.ReadByte();

            Users.Add(new ServerWorldListUser
            {
                Class = (CharacterClass)(classWithFlags & 0x07),
                Flags = (byte)(classWithFlags & 0xF8),
                Color = (WorldListColor)reader.ReadByte(),
                Status = (SocialStatus)reader.ReadByte(),
                Title = reader.ReadString8(),
                IsMaster = reader.ReadBoolean(),
                Name = reader.ReadString8()
            });
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt16(WorldCount);
        builder.AppendUInt16(CountryCount);

        foreach (var user in Users)
        {
            var classWithFlags = (byte)((byte)user.Class | user.Flags);
            builder.AppendByte(classWithFlags);
            builder.AppendByte((byte)user.Color);
            builder.AppendByte((byte)user.Status);
            builder.AppendString8(user.Title);
            builder.AppendBoolean(user.IsMaster);
            builder.AppendString8(user.Name);
        }
    }
}
