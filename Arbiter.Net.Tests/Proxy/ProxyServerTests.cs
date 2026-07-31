using System.Net;
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

    [Test]
    public async Task Should_Use_Updated_Remote_Endpoint_For_New_Connections()
    {
        using var firstRemoteListener = new TcpListener(IPAddress.Loopback, 0);
        using var secondRemoteListener = new TcpListener(IPAddress.Loopback, 0);
        firstRemoteListener.Start();
        secondRemoteListener.Start();

        var firstRemoteEndpoint = (IPEndPoint)firstRemoteListener.LocalEndpoint;
        var secondRemoteEndpoint = (IPEndPoint)secondRemoteListener.LocalEndpoint;

        using var server = new ProxyServer();
        server.Start(0, firstRemoteEndpoint);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var firstProxyClient = new TcpClient();
        await firstProxyClient.ConnectAsync(server.LocalEndpoint!, timeout.Token);
        using var firstRemoteClient = await firstRemoteListener.AcceptTcpClientAsync(timeout.Token);

        server.SetRemoteEndpoint(secondRemoteEndpoint);

        using var secondProxyClient = new TcpClient();
        await secondProxyClient.ConnectAsync(server.LocalEndpoint!, timeout.Token);
        using var secondRemoteClient = await secondRemoteListener.AcceptTcpClientAsync(timeout.Token);

        Assert.Multiple(() =>
        {
            Assert.That(firstRemoteClient.Connected, Is.True);
            Assert.That(secondRemoteClient.Connected, Is.True);
            Assert.That(server.RemoteEndpoint, Is.EqualTo(secondRemoteEndpoint));
        });
    }

    [Test]
    public async Task Should_Close_Both_Sockets_When_Client_Disconnects()
    {
        using var remoteListener = new TcpListener(IPAddress.Loopback, 0);
        remoteListener.Start();

        var remoteEndpoint = (IPEndPoint)remoteListener.LocalEndpoint;
        using var server = new ProxyServer();
        var clientDisconnected = new TaskCompletionSource<ProxyConnection>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        server.ClientDisconnected += (_, e) => clientDisconnected.TrySetResult(e.Connection);
        server.Start(0, remoteEndpoint);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var proxyClient = new TcpClient();
        await proxyClient.ConnectAsync(server.LocalEndpoint!, timeout.Token);
        using var remoteClient = await remoteListener.AcceptTcpClientAsync(timeout.Token);

        proxyClient.Close();

        var disconnectedConnection = await clientDisconnected.Task.WaitAsync(timeout.Token);
        while (server.Connections.Any())
        {
            await Task.Delay(10, timeout.Token);
        }

        var buffer = new byte[1];
        var remoteReadCount = await remoteClient.GetStream().ReadAsync(buffer, timeout.Token);

        Assert.Multiple(() =>
        {
            Assert.That(disconnectedConnection.IsClientConnected, Is.False);
            Assert.That(disconnectedConnection.IsServerConnected, Is.False);
            Assert.That(remoteReadCount, Is.Zero);
        });
    }
}
