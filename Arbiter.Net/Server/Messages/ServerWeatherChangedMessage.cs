using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.WeatherChanged)]
public class ServerWeatherChangedMessage : ServerMessage
{
    public byte WeatherFlags { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        WeatherFlags = reader.ReadByte();
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte(WeatherFlags);
    }
}
