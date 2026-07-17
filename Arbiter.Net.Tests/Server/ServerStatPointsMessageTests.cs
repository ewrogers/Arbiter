using Arbiter.Net.Serialization;
using Arbiter.Net.Server;
using Arbiter.Net.Server.Messages;

namespace Arbiter.Net.Tests.Server;

public sealed class ServerStatPointsMessageTests
{
    [Test]
    public void Should_Map_StatPoints_Command_And_Round_Trip_Fields_In_Wire_Order()
    {
        const byte points = 12;
        var packet = new ServerPacket((byte)ServerCommand.LevelPoint, [1, points]);
        var factory = new ServerMessageFactory();

        var created = factory.Create(packet);
        var message = created as ServerLevelPointMessage;

        Assert.That(message, Is.Not.Null);

        var builder = new NetworkPacketBuilder(ServerCommand.LevelPoint);
        message!.Serialize(ref builder);
        var serialized = builder.ToPacket();
        builder.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That((byte)ServerCommand.LevelPoint, Is.EqualTo(0x3D));
            Assert.That(factory.GetMessageType(ServerCommand.LevelPoint),
                Is.EqualTo(typeof(ServerLevelPointMessage)));
            Assert.That(factory.GetMessageCommand(typeof(ServerLevelPointMessage)),
                Is.EqualTo(ServerCommand.LevelPoint));
            Assert.That(message.FlashButtons, Is.True);
            Assert.That(message.StatPoints, Is.EqualTo(points));
            Assert.That(serialized.Data, Is.EqualTo(new byte[] { 1, points }));
        });
    }
}
