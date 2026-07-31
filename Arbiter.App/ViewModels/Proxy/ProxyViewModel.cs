using System;
using System.Linq;
using System.Net;
using Arbiter.App.Services.Entities;
using Arbiter.App.Services.Players;
using Arbiter.Net.Proxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel : ViewModelBase
{
    private readonly ILogger<ProxyViewModel> _logger;
    private readonly ProxyServer _proxyServer;
    private readonly IPlayerService _playerService;
    private readonly IEntityStore _entityStore;
    
    public bool IsRunning => _proxyServer.IsRunning;

    public ProxyViewModel(ILogger<ProxyViewModel> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        
        _proxyServer = serviceProvider.GetRequiredService<ProxyServer>();
        _playerService = serviceProvider.GetRequiredService<IPlayerService>();
        _entityStore = serviceProvider.GetRequiredService<IEntityStore>();
    }

    public void Start(int localPort, IPAddress remoteIpAddress, int remotePort)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Proxy is already running.");
        }

        _proxyServer.ClientConnected += OnClientConnected;
        _proxyServer.ServerConnected += OnServerConnected;
        _proxyServer.ClientDisconnected += OnClientDisconnected;
        _proxyServer.ServerDisconnected += OnServerDisconnected;
        _proxyServer.ClientAuthenticated += OnClientAuthenticated;
        _proxyServer.ClientLoggedIn += OnClientLoggedIn;
        _proxyServer.ClientLoggedOut += OnClientLoggedOut;
        _proxyServer.ClientRedirected += OnClientRedirected;
        _proxyServer.PacketException += OnClientException;
        _proxyServer.FilterException += OnFilterException;

        _proxyServer.Start(localPort, remoteIpAddress, remotePort);
        OnPropertyChanged(nameof(IsRunning));

        _logger.LogInformation("Proxy started on 127.0.0.1:{Port}", localPort);
    }

    public void SetRemoteEndpoint(IPAddress remoteIpAddress, int remotePort)
    {
        _proxyServer.SetRemoteEndpoint(remoteIpAddress, remotePort);
        _logger.LogInformation("Proxy remote server updated to {Endpoint}", _proxyServer.RemoteEndpoint);
    }

    private void OnClientConnected(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Client connected -> {Endpoint}", name, e.Connection.LocalEndpoint);

        // Create the interact request queue on connection
        _interactRequests.AddOrUpdate(e.Connection.Id, [], (_, _) => []);
    }

    private void OnServerConnected(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Server connected <- {Endpoint}", name, e.Connection.RemoteEndpoint);
    }

    private void OnClientDisconnected(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Client disconnected -> {Endpoint}", name, e.Connection.LocalEndpoint);

        // Remove the interact request queue on connection
        _interactRequests.TryRemove(e.Connection.Id, out _);
    }

    private void OnServerDisconnected(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Server disconnected <- {Endpoint}", name, e.Connection.RemoteEndpoint);
    }

    private void OnClientAuthenticated(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Client authenticated", name);
    }

    private void OnClientLoggedIn(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Client logged in", name);
    }

    private void OnClientLoggedOut(object? sender, ProxyConnectionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Client logged out", name);
    }

    private void OnClientRedirected(object? sender, ProxyConnectionRedirectEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        _logger.LogInformation("[{Name}] Client redirected -> {Endpoint}", name, e.RemoteEndpoint);
    }

    private void OnClientException(object? sender, ProxyConnectionExceptionEventArgs e)
    {
        var name = e.Connection.Name ?? e.Connection.Id.ToString();

        var packetString = string.Join(' ', e.Packet.Data.Select(b => b.ToString("X2")));
        _logger.LogWarning("[{Name}] Bad packet: {Packet}", name, packetString);
    }

    private void OnFilterException(object? sender, ProxyConnectionFilterEventArgs e)
    {
        if (e.Result.Exception is not null)
        {
            return;
        }

        var name = e.Connection.Name ?? e.Connection.Id.ToString();
        var lastFilter = e.Result.FilterChain.LastOrDefault();

        _logger.LogError(e.Result.Exception, "[{Name}] Filter ({FilterName}) exception: {Message}", name, lastFilter,
            e.Result.Exception!.Message);
    }
}
