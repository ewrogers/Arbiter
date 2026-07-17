using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.DrawObjects)]
public class ServerDrawObjectsMessage : ServerMessage
{
    public List<ServerEntityObject> Entities { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        var entityCount = reader.ReadUInt16();

        for (var i = 0; i < entityCount; i++)
        {
            var x = reader.ReadUInt16();
            var y = reader.ReadUInt16();
            var id = reader.ReadUInt32();
            var sprite = reader.ReadUInt16();

            if (SpriteFlags.IsCreature(sprite))
            {
                var unknown = reader.ReadUInt32();
                var direction = (WorldDirection)reader.ReadByte();

                reader.Skip(1); // not sure what this is for

                var creatureType = (CreatureType)reader.ReadByte();
                var creatureName = creatureType == CreatureType.Mundane ? reader.ReadString8() : null;

                Entities.Add(new ServerCreatureEntity
                {
                    Id = id,
                    X = x,
                    Y = y,
                    Sprite = SpriteFlags.ClearFlags(sprite),
                    Direction = direction,
                    CreatureType = creatureType,
                    Name = creatureName,
                    Unknown = unknown,
                });
            }
            else if (SpriteFlags.IsItem(sprite))
            {
                var color = (DyeColor)reader.ReadByte();
                var unknown = reader.ReadUInt16();

                Entities.Add(new ServerItemEntity
                {
                    Id = id,
                    X = x,
                    Y = y,
                    Sprite = SpriteFlags.ClearFlags(sprite),
                    Color = color,
                    Unknown = unknown,
                });
            }
            else
            {
                throw new InvalidOperationException("Invalid entity type");
            }
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        var entityCount = Entities.Count;
        builder.AppendUInt16((ushort)entityCount);

        for (var i = 0; i < entityCount; i++)
        {
            var entity = Entities[i];

            builder.AppendUInt16(entity.X);
            builder.AppendUInt16(entity.Y);
            builder.AppendUInt32(entity.Id);

            var spriteWithFlags = entity switch
            {
                ServerCreatureEntity creature => SpriteFlags.SetCreature(creature.Sprite),
                ServerItemEntity item => SpriteFlags.SetItem(item.Sprite),
                _ => entity.Sprite
            };
            
            builder.AppendUInt16(spriteWithFlags);

            switch (entity)
            {
                case ServerCreatureEntity creature:
                {
                    builder.AppendUInt32(creature.Unknown);
                    builder.AppendByte((byte)creature.Direction);
                    builder.AppendByte(0x00); // not sure what this is for
                    builder.AppendByte((byte)creature.CreatureType);

                    if (creature.CreatureType == CreatureType.Mundane)
                    {
                        builder.AppendString8(creature.Name ?? string.Empty);
                    }

                    break;
                }
                case ServerItemEntity item:
                    builder.AppendByte((byte)item.Color);
                    builder.AppendUInt16(item.Unknown);
                    break;
            }
        }
    }
}
