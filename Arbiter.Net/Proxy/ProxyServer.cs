using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Arbiter.Net.Client;
using Arbiter.Net.Filters;

namespace Arbiter.Net.Proxy;

public partial class ProxyServer : IDisposable
{
    private bool _isDisposed;
    private IPEndPoint? _remoteEndpoint;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancelTokenSource;

    private readonly Lock _connectionsLock = new();
    private readonly List<ProxyConnection> _connections = [];
    private int _nextConnectionId;
    private readonly ConcurrentQueue<IPEndPoint> _pendingRedirects = new();

    public bool IsRunning => _listener is not null;
    public IPEndPoint? LocalEndpoint => _listener?.LocalEndpoint as IPEndPoint;
    public IPEndPoint? RemoteEndpoint => _remoteEndpoint;

    public event Action? Started;
    public event Action? Stopped;
    public event EventHandler<ProxyConnectionEventArgs>? ClientConnected;
    public event EventHandler<ProxyConnectionEventArgs>? ServerConnected;
    public event EventHandler<ProxyConnectionEventArgs>? ClientAuthenticated;
    public event EventHandler<ProxyConnectionEventArgs>? ClientLoggedIn;
    public event EventHandler<ProxyConnectionEventArgs>? ClientLoggedOut;
    public event EventHandler<ProxyConnectionEventArgs>? ClientDisconnected;
    public event EventHandler<ProxyConnectionEventArgs>? ServerDisconnected;
    public event EventHandler<ProxyConnectionRedirectEventArgs>? ClientRedirected;
    public event EventHandler<ProxyConnectionDataEventArgs>? PacketReceived;
    public event EventHandler<ProxyConnectionDataEventArgs>? PacketSent;
    public event EventHandler<ProxyConnectionDataEventArgs>? PacketQueued;
    public event EventHandler<ProxyConnectionExceptionEventArgs>? PacketException;
    public event EventHandler<ProxyConnectionFilterEventArgs>? FilterException;

    public IEnumerable<ProxyConnection> Connections => _connections;

    public void Start(int listenPort, IPAddress remoteAddress, int remotePort) =>
        Start(listenPort, new IPEndPoint(remoteAddress, remotePort));

    public void Start(int listenPort, IPEndPoint remoteEndpoint)
    {
        CheckIfDisposed();

        if (IsRunning)
        {
            throw new InvalidOperationException("Proxy server is already running");
        }

        _remoteEndpoint = remoteEndpoint;

        _cancelTokenSource = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, listenPort);
        _listener.Start();
        
        _ = AcceptLoopAsync(_cancelTokenSource.Token);

        Started?.Invoke();
    }

    public void Stop()
    {
        CheckIfDisposed();

        if (!IsRunning)
        {
            return;
        }

        _cancelTokenSource?.Cancel();
        _listener?.Stop();
        _listener = null;

        Stopped?.Invoke();
    }

    private async Task AcceptLoopAsync(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener!.AcceptTcpClientAsync(token).ConfigureAwait(false);
            }
            catch when (token.IsCancellationRequested)
            {
                break;
            }

            var connectionId = Interlocked.Increment(ref _nextConnectionId);
            var connection = new ProxyConnection(connectionId, client);
            {
                using var _ = _connectionsLock.EnterScope();
                _connections.Add(connection);
            }

            // Inherit proxy-wide filters for newly connected client
            InheritFilters(connection);
            // Inherit proxy-wide observers for newly connected client
            InheritObservers(connection);

            ClientConnected?.Invoke(this, new ProxyConnectionEventArgs(connection));

            _ = HandleConnectionAsync(connection, token);
        }
    }

    private async Task HandleConnectionAsync(ProxyConnection connection, CancellationToken token)
    {
        connection.ClientAuthenticated += OnClientAuthenticated;
        connection.ClientLoggedIn += OnClientLoggedIn;
        connection.ClientLoggedOut += OnClientLoggedOut;
        connection.ClientDisconnected += OnClientDisconnected;
        connection.ServerConnected += OnServerConnected;
        connection.ServerDisconnected += OnServerDisconnected;
        connection.ClientRedirected += OnClientRedirected;
        connection.PacketReceived += OnRecv;
        connection.PacketSent += OnSend;
        connection.PacketQueued += OnQueued;
        connection.PacketException += OnExceptionPacket;
        connection.FilterException += OnFilterException;

        // Check for any pending redirects, use that first. The replacement connection stays in transfer
        // until the client sends its TransferServer packet.
        var isRedirected = _pendingRedirects.TryDequeue(out var endpoint);
        var remoteEndpoint = isRedirected ? endpoint! : RemoteEndpoint!;
        if (isRedirected)
        {
            connection.BeginTransfer();
        }

        try
        {
            await connection.ConnectToRemoteAsync(remoteEndpoint, token).ConfigureAwait(false);
            await connection.SendRecvLoopAsync(token).ConfigureAwait(false);
        }
        finally
        {
            connection.ClientAuthenticated -= OnClientAuthenticated;
            connection.ClientLoggedIn -= OnClientLoggedIn;
            connection.ClientLoggedOut -= OnClientLoggedOut;
            connection.ClientDisconnected -= OnClientDisconnected;
            connection.ServerConnected -= OnServerConnected;
            connection.ServerDisconnected -= OnServerDisconnected;
            connection.ClientRedirected -= OnClientRedirected;
            connection.PacketReceived -= OnRecv;
            connection.PacketSent -= OnSend;
            connection.PacketQueued -= OnQueued;
            connection.PacketException -= OnExceptionPacket;
            connection.FilterException -= OnFilterException;

            using var _ = _connectionsLock.EnterScope();
            _connections.Remove(connection);
            connection.Dispose();
        }
    }

    private void OnClientAuthenticated(object? sender, EventArgs e) =>
        ClientAuthenticated?.Invoke(this, new ProxyConnectionEventArgs((sender as ProxyConnection)!));

    private void OnClientLoggedIn(object? sender, EventArgs e) =>
        ClientLoggedIn?.Invoke(this, new ProxyConnectionEventArgs((sender as ProxyConnection)!));

    private void OnClientLoggedOut(object? sender, EventArgs e) =>
        ClientLoggedOut?.Invoke(this, new ProxyConnectionEventArgs((sender as ProxyConnection)!));

    private void OnClientDisconnected(object? sender, EventArgs e) =>
        ClientDisconnected?.Invoke(this, new ProxyConnectionEventArgs((sender as ProxyConnection)!));

    private void OnServerConnected(object? sender, EventArgs e) =>
        ServerConnected?.Invoke(this, new ProxyConnectionEventArgs((sender as ProxyConnection)!));

    private void OnServerDisconnected(object? sender, EventArgs e) =>
        ServerDisconnected?.Invoke(this, new ProxyConnectionEventArgs((sender as ProxyConnection)!));

    private void OnRecv(object? sender, NetworkTransferEventArgs e)
        => PacketReceived?.Invoke(this,
            new ProxyConnectionDataEventArgs((sender as ProxyConnection)!, e.Direction, e.Encrypted, e.Decrypted,
                e.FilterResult));

    private void OnSend(object? sender, NetworkTransferEventArgs e) =>
        PacketSent?.Invoke(this,
            new ProxyConnectionDataEventArgs((sender as ProxyConnection)!, e.Direction, e.Decrypted, e.Decrypted,
                e.FilterResult));

    private void OnQueued(object? sender, NetworkPacketEventArgs e) =>
        PacketQueued?.Invoke(this, new ProxyConnectionDataEventArgs((sender as ProxyConnection)!,
            e.Packet is ClientPacket ? NetworkDirection.Send : NetworkDirection.Receive,
            e.Packet, e.Packet));

    private void OnExceptionPacket(object? sender, NetworkPacketEventArgs e) =>
        PacketException?.Invoke(this,
            new ProxyConnectionExceptionEventArgs((sender as ProxyConnection)!, e.Packet));

    private void OnFilterException(object? sender, NetworkFilterEventArgs e) =>
        FilterException?.Invoke(this, new ProxyConnectionFilterEventArgs((sender as ProxyConnection)!, e.Result));

    private void OnClientRedirected(object? sender, NetworkRedirectEventArgs e)
    {
        _pendingRedirects.Enqueue(e.RemoteEndpoint);

        ClientRedirected?.Invoke(this,
            new ProxyConnectionRedirectEventArgs((sender as ProxyConnection)!, e.Name, e.RemoteEndpoint));
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
            _cancelTokenSource?.Cancel();
            _listener?.Stop();

            using var _ = _connectionsLock.EnterScope();
            foreach (var connection in _connections)
            {
                connection.Dispose();
            }

            _connections.Clear();
        }

        _listener = null;
        _isDisposed = true;
    }

    private void CheckIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
}
