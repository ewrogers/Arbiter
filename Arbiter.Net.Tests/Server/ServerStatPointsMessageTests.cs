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
        var packet = new ServerPacket((byte)ServerCommand.StatPoints, [1, points]);
        var factory = new ServerMessageFactory();

        var created = factory.Create(packet);
        var message = created as ServerStatPointsMessage;

        Assert.That(message, Is.Not.Null);

        var builder = new NetworkPacketBuilder(ServerCommand.StatPoints);
        message!.Serialize(ref builder);
        var serialized = builder.ToPacket();
        builder.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That((byte)ServerCommand.StatPoints, Is.EqualTo(0x3D));
            Assert.That(factory.GetMessageType(ServerCommand.StatPoints),
                Is.EqualTo(typeof(ServerStatPointsMessage)));
            Assert.That(factory.GetMessageCommand(typeof(ServerStatPointsMessage)),
                Is.EqualTo(ServerCommand.StatPoints));
            Assert.That(message.FlashButtons, Is.True);
            Assert.That(message.StatPoints, Is.EqualTo(points));
            Assert.That(serialized.Data, Is.EqualTo(new byte[] { 1, points }));
        });
    }
}
