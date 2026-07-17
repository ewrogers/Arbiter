using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.UserAppearance)]
public class ServerUserAppearanceMessage : ServerMessage
{
    public uint UserId { get; set; }
    public WorldDirection Direction { get; set; }
    public bool HasGuild { get; set; }
    public CharacterClass Class { get; set; }
    public bool CanMove { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        UserId = reader.ReadUInt32();
        Direction = (WorldDirection)reader.ReadByte();

        HasGuild = reader.ReadBoolean();

        Class = (CharacterClass)reader.ReadByte();
        CanMove = (reader.ReadByte() & 1) == 0; // seems to be a bit flag but not sure what else it affects
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendUInt32(UserId);
        builder.AppendByte((byte)Direction);

        builder.AppendBoolean(HasGuild);

        builder.AppendByte((byte)Class);
        builder.AppendByte((byte)(CanMove ? 0 : 1));
    }
}
