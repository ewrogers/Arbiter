using System;
using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Player;
using Arbiter.Net;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Client;

public partial class ClientViewModel : ViewModelBase
{
    [ObservableProperty] private bool _shouldBlockNextRedirect;
    
    public int Id { get; init; }
    public required string Name { get; set; }

    private readonly ProxyConnection _connection;
    private bool _isSubscribed;

    public ProxyConnection Connection => _connection;
    
    public PlayerViewModel Player { get; }

    public ClientViewModel(ProxyConnection connection, PlayerState player)
        : this(connection, player, NullGameSpriteService.Instance, TimeProvider.System)
    {
    }

    public ClientViewModel(
        ProxyConnection connection,
        PlayerState player,
        IGameSpriteService spriteService,
        TimeProvider timeProvider)
    {
        _connection = connection;
        Player = new PlayerViewModel(player, spriteService, timeProvider);
    }

    public event EventHandler? BringToFrontRequested;

    public void Subscribe()
    {
        if (_isSubscribed)
        {
            return;
        }

        RegisterFilters();
        Player.Subscribe(_connection);
        _isSubscribed = true;
    }

    public void Unsubscribe()
    {
        if (!_isSubscribed)
        {
            return;
        }

        Player.Unsubscribe();
        UnregisterFilters();
        _isSubscribed = false;
    }

    public void SendBarMessage(string message, WorldMessageType messageType = WorldMessageType.BarMessage)
    {
        EnqueueMessage(new ServerSystemMessage
        {
            MessageType = messageType,
            Message = message,
        });
    }

    public bool EnqueuePacket(NetworkPacket packet, NetworkPriority priority = NetworkPriority.Normal) =>
        _connection.EnqueuePacket(packet, priority);

    public bool EnqueueMessage(IClientMessage packet, NetworkPriority priority = NetworkPriority.Normal) =>
        _connection.EnqueueMessage(packet, priority);

    public bool EnqueueMessage(IServerMessage packet, NetworkPriority priority = NetworkPriority.Normal) =>
        _connection.EnqueueMessage(packet, priority);

    private static bool CanBringToFront() => OperatingSystem.IsWindows();

    [RelayCommand(CanExecute = nameof(CanBringToFront))]
    private void BringToFront()
    {
        BringToFrontRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanDisconnect() => _connection.IsConnected;

    [RelayCommand(CanExecute = nameof(CanDisconnect))]
    public void Disconnect()
    {
        _connection.Disconnect();
        DisconnectCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public void ToggleBlockNextRedirect(bool isCancel = false)
    {
        ShouldBlockNextRedirect = !isCancel;
    }
}
