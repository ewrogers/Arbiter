using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.StaticObjectState)]
public class ServerStaticObjectStateMessage : ServerMessage
{
    public List<ServerMapDoor> Doors { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);
        
        var count = reader.ReadByte();

        for (var i = 0; i < count; i++)
        {
            var door = new ServerMapDoor
            {
                X = reader.ReadByte(),
                Y = reader.ReadByte(),
                State = (DoorState)reader.ReadByte(),
                Direction = (DoorDirection)reader.ReadByte()
            };

            Doors.Add(door);
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Doors.Count);
        
        foreach (var door in Doors)
        {
            builder.AppendByte(door.X);
            builder.AppendByte(door.Y);
            builder.AppendByte((byte)door.State);
            builder.AppendByte((byte)door.Direction);
        }
    }
}
