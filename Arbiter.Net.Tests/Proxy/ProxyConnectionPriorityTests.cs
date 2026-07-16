using Arbiter.Net.Client;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server;
using System.Net.Sockets;

namespace Arbiter.Net.Tests.Proxy;

public class ProxyConnectionPriorityTests
{
    [TestCase(ClientCommand.ReplyCRC)]
    [TestCase(ClientCommand.CheckTime)]
    public void Should_Prioritize_Client_Heartbeat_And_Check_Time_Packets(ClientCommand command)
    {
        var packet = new ClientPacket((byte)command, Array.Empty<byte>());

        var priority = ProxyConnection.ResolvePacketPriority(packet);

        Assert.That(priority, Is.EqualTo(NetworkPriority.High));
    }

    [TestCase(ServerCommand.RequestCRC)]
    [TestCase(ServerCommand.CheckTime)]
    public void Should_Prioritize_Server_Heartbeat_And_Check_Time_Packets(ServerCommand command)
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
            new ClientPacket((byte)ClientCommand.Move, Array.Empty<byte>()),
            new ServerPacket((byte)ServerCommand.Message, Array.Empty<byte>())
        ];

        foreach (var packet in packets)
        {
            Assert.That(ProxyConnection.ResolvePacketPriority(packet), Is.EqualTo(NetworkPriority.Normal));
        }
    }

    [Test]
    public void Should_Preserve_Explicit_High_Priority()
    {
        var packet = new ClientPacket((byte)ClientCommand.Move, Array.Empty<byte>());

        var priority = ProxyConnection.ResolvePacketPriority(packet, NetworkPriority.High);

        Assert.That(priority, Is.EqualTo(NetworkPriority.High));
    }

    [TestCase(ClientCommand.ReplyCRC)]
    [TestCase(ClientCommand.CheckTime)]
    public void Should_Block_Client_Heartbeats_During_Transfer(ClientCommand command)
    {
        using var client = new TcpClient();
        using var connection = new ProxyConnection(1, client);
        connection.BeginTransfer();

        var packet = new ClientPacket((byte)command, Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(connection.IsTransferring, Is.True);
            Assert.That(connection.ShouldBlockClientHeartbeatDuringTransfer(packet), Is.True);
        });
    }

    [Test]
    public void Should_Allow_Client_Heartbeats_After_Transfer()
    {
        using var client = new TcpClient();
        using var connection = new ProxyConnection(1, client);
        connection.BeginTransfer();
        connection.CompleteTransfer();

        var packet = new ClientPacket((byte)ClientCommand.ReplyCRC, Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(connection.IsTransferring, Is.False);
            Assert.That(connection.ShouldBlockClientHeartbeatDuringTransfer(packet), Is.False);
        });
    }
}
