using System.Net.Sockets;
using System.Reflection;
using Arbiter.Net.Proxy;

namespace Arbiter.Net.Tests.Proxy;

public class ProxyServerTests
{
    [Test]
    public void Should_Return_A_Stable_Connection_Snapshot()
    {
        using var server = new ProxyServer();
        using var client = new TcpClient();
        using var connection = new ProxyConnection(1, client);

        var connectionsField = typeof(ProxyServer).GetField("_connections",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var connections = connectionsField?.GetValue(server) as List<ProxyConnection>;

        Assert.That(connections, Is.Not.Null);
        connections!.Add(connection);

        var snapshot = server.Connections;
        connections.Clear();

        Assert.That(snapshot, Is.EqualTo(new[] { connection }));
        Assert.That(server.Connections, Is.Empty);
    }
}
