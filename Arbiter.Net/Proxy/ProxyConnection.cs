using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Arbiter.Net.Client;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Filters;
using Arbiter.Net.Server;
using Arbiter.Net.Server.Messages;

namespace Arbiter.Net.Proxy;

public partial class ProxyConnection : IDisposable
{
    private const int RecvBufferSize = 4096;

    private bool _isDisposed;
    private readonly TcpClient _client;
    private readonly ClientMessageFactory _clientMessageFactory;
    private readonly ServerMessageFactory _serverMessageFactory;

    private NetworkStream? _clientStream;

    private readonly NetworkPacketBuffer _clientPacketBuffer = new((command, data) => new ClientPacket(command, data)
    {
        Sequence = ClientPacketEncryptor.IsEncrypted(command) ? data[0] : null
    });

    private readonly ClientPacketEncryptor _clientEncryptor = new();

    private TcpClient? _server;
    private NetworkStream? _serverStream;
    private readonly NetworkPacketBuffer _serverPacketBuffer = new((command, data) => new ServerPacket(command, data));
    private readonly ServerPacketEncryptor _serverEncryptor = new();

    private int _clientSequence;
    private int _serverSequence;
    private int _isTransferring;
    private readonly Channel<NetworkPacket> _sendQueue = Channel.CreateUnbounded<NetworkPacket>();
    private readonly Channel<NetworkPacket> _prioritySendQueue = Channel.CreateUnbounded<NetworkPacket>();

    public int Id { get; }
    public string? Name { get; private set; }
    public long? UserId { get; set; }
    public bool HasAuthenticated { get; private set; }
    public bool IsLoggedIn { get; private set; }
    public bool IsTransferring => Volatile.Read(ref _isTransferring) != 0;

    public IPEndPoint? LocalEndpoint => _client.Client.LocalEndPoint as IPEndPoint;
    public IPEndPoint? RemoteEndpoint => _server?.Client.RemoteEndPoint as IPEndPoint;
    public bool IsConnected => IsClientConnected && IsServerConnected;
    public bool IsClientConnected => _client.Connected;
    public bool IsServerConnected => _server?.Connected ?? false;

    public event EventHandler? ClientAuthenticated;
    public event EventHandler? ClientLoggedIn;
    public event EventHandler? ClientLoggedOut;
    public event EventHandler? ClientDisconnected;
    public event EventHandler? ServerConnected;
    public event EventHandler? ServerDisconnected;

    public event EventHandler<NetworkRedirectEventArgs>? ClientRedirected;
    public event EventHandler<NetworkTransferEventArgs>? PacketReceived;
    public event EventHandler<NetworkTransferEventArgs>? PacketSent;
    public event EventHandler<NetworkPacketEventArgs>? PacketQueued;
    public event EventHandler<NetworkPacketEventArgs>? PacketException;
    public event EventHandler<NetworkFilterEventArgs>? FilterException;

    public ProxyConnection(int id, TcpClient client, ClientMessageFactory? clientMessageFactory = null,
        ServerMessageFactory? serverMessageFactory = null)
    {
        Id = id;

        _client = client;
        _client.NoDelay = true;

        _clientMessageFactory = clientMessageFactory ?? ClientMessageFactory.Default;
        _serverMessageFactory = serverMessageFactory ?? ServerMessageFactory.Default;
    }

    internal async Task ConnectToRemoteAsync(IPEndPoint remoteEndpoint, CancellationToken token = default)
    {
        // Create the TCP client to connect to the remote server
        _server = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
        await _server.ConnectAsync(remoteEndpoint, linked.Token).ConfigureAwait(false);

        // Get the client and server network streams
        _clientStream = _client.GetStream();
        _serverStream = _server.GetStream();

        ServerConnected?.Invoke(this, EventArgs.Empty);
    }

    internal Task SendRecvLoopAsync(CancellationToken token = default)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(token);

        // Start the client/server recv and send queue tasks in the background
        var clientRecvTask = RecvLoopAsync(_clientStream!, _clientPacketBuffer, _clientEncryptor,
            ProxyDirection.ClientToServer, linked);
        var serverRecvTask = RecvLoopAsync(_serverStream!, _serverPacketBuffer, _serverEncryptor,
            ProxyDirection.ServerToClient, linked);
        var senderTask = SendLoopAsync(linked.Token);

        return Task.WhenAll(clientRecvTask, serverRecvTask, senderTask);
    }

    public void Disconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        _client.Close();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool isDisposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (isDisposing)
        {
            _sendQueue.Writer.TryComplete();
            _prioritySendQueue.Writer.TryComplete();

            _clientStream?.Dispose();
            _client.Dispose();

            _serverStream?.Dispose();
            _server?.Dispose();
        }

        _isDisposed = true;
    }

    private void CheckIfDisposed()
        => ObjectDisposedException.ThrowIf(_isDisposed, this);
}
