using Arbiter.Net.Serialization;
using Arbiter.Net.Server;
using Arbiter.Net.Server.Messages;

namespace Arbiter.Net.Tests.Server;

public sealed class ServerWeatherChangedMessageTests
{
    [Test]
    public void Should_Map_WeatherChanged_Command_And_Round_Trip_Weather_Flags()
    {
        const byte weatherFlags = 0xA5;
        var packet = new ServerPacket((byte)ServerCommand.WeatherChanged, [weatherFlags]);
        var factory = new ServerMessageFactory();

        var created = factory.Create(packet);
        var message = created as ServerWeatherChangedMessage;

        Assert.That(message, Is.Not.Null);

        var builder = new NetworkPacketBuilder(ServerCommand.WeatherChanged);
        message!.Serialize(ref builder);
        var serialized = builder.ToPacket();
        builder.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That((byte)ServerCommand.WeatherChanged, Is.EqualTo(0x1F));
            Assert.That(factory.GetMessageType(ServerCommand.WeatherChanged),
                Is.EqualTo(typeof(ServerWeatherChangedMessage)));
            Assert.That(factory.GetMessageCommand(typeof(ServerWeatherChangedMessage)),
                Is.EqualTo(ServerCommand.WeatherChanged));
            Assert.That(message.WeatherFlags, Is.EqualTo(weatherFlags));
            Assert.That(serialized.Data, Is.EqualTo(new byte[] { weatherFlags }));
        });
    }
}
