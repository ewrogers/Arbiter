using Arbiter.Net.Client;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server;

namespace Arbiter.Net.Tests.Proxy;

public class ProxyConnectionPriorityTests
{
    [TestCase(ClientCommand.Heartbeat)]
    [TestCase(ClientCommand.SyncTicks)]
    public void Should_Prioritize_Client_Heartbeat_And_Tick_Sync_Packets(ClientCommand command)
    {
        var packet = new ClientPacket((byte)command, Array.Empty<byte>());

        var priority = ProxyConnection.ResolvePacketPriority(packet);

        Assert.That(priority, Is.EqualTo(NetworkPriority.High));
    }

    [TestCase(ServerCommand.Heartbeat)]
    [TestCase(ServerCommand.SyncTicks)]
    public void Should_Prioritize_Server_Heartbeat_And_Tick_Sync_Packets(ServerCommand command)
    {
        var packet = new ServerPacket((byte)command, Array.Empty<byte>());

        var priority = ProxyConnection.ResolvePacketPriority(packet);

        Assert.That(priority, Is.EqualTo(NetworkPriority.High));
    }

    [Test]
    public void Should_Not_Prioritize_Normal_Packets()
    {
        NetworkPacket[] packets =
        [
            new ClientPacket((byte)ClientCommand.Walk, Array.Empty<byte>()),
            new ServerPacket((byte)ServerCommand.WorldMessage, Array.Empty<byte>())
        ];

        foreach (var packet in packets)
        {
            Assert.That(ProxyConnection.ResolvePacketPriority(packet), Is.EqualTo(NetworkPriority.Normal));
        }
    }

    [Test]
    public void Should_Preserve_Explicit_High_Priority()
    {
        var packet = new ClientPacket((byte)ClientCommand.Walk, Array.Empty<byte>());

        var priority = ProxyConnection.ResolvePacketPriority(packet, NetworkPriority.High);

        Assert.That(priority, Is.EqualTo(NetworkPriority.High));
    }
}
