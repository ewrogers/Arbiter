using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Client;
using Arbiter.Net.Proxy;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Dialogs;

public partial class DialogManagerViewModel : ViewModelBase
{
    private readonly ILogger<DialogManagerViewModel> _logger;
    private readonly ProxyServer _proxyServer;
    private readonly ClientManagerViewModel _clientManager;
    private readonly IGameSpriteService _spriteService;

    private readonly ConcurrentDictionary<long, DialogViewModel?> _activeDialogs = [];

    [ObservableProperty] private DialogViewModel? _activeDialog;

    [ObservableProperty] private bool _hasClients;

    [ObservableProperty] private ClientViewModel? _selectedClient;

    [ObservableProperty] private bool _shouldSync = true;

    public DialogManagerViewModel(ILogger<DialogManagerViewModel> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _clientManager = serviceProvider.GetRequiredService<ClientManagerViewModel>();
        _spriteService = serviceProvider.GetRequiredService<IGameSpriteService>();

        _clientManager.Clients.CollectionChanged += OnClientsCollectionChanged;
        _clientManager.ClientSelected += OnClientSelected;
        _clientManager.ClientDisconnected += OnClientDisconnected;
        _spriteService.SpritesChanged += OnSpritesChanged;

        _proxyServer = serviceProvider.GetRequiredService<ProxyServer>();
        AddObservers();
    }

    private void OnClientSelected(ClientViewModel? client)
    {
        SelectedClient = client;
    }

    private void OnClientDisconnected(ClientViewModel client) =>
        _activeDialogs.TryRemove(client.Id, out _);

    private void OnClientsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not ObservableCollection<ClientViewModel> collection)
        {
            return;
        }

        HasClients = collection.Count > 0;

        if (e.Action != NotifyCollectionChangedAction.Remove)
        {
            return;
        }

        // If the currently selected client was removed from the collection, clear the selection
        if (SelectedClient is null || collection.Contains(SelectedClient))
        {
            return;
        }

        SelectedClient = null;

        if (ShouldSync)
        {
            ActiveDialog = null;
        }
    }

    private void OnSpritesChanged(object? sender, EventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnSpritesChanged(sender, e));
            return;
        }

        var dialogs = _activeDialogs.Values.OfType<DialogViewModel>().ToHashSet();
        if (ActiveDialog is not null)
        {
            dialogs.Add(ActiveDialog);
        }

        foreach (var dialog in dialogs)
        {
            dialog.RefreshSprite();
        }
    }

    partial void OnSelectedClientChanged(ClientViewModel? oldValue, ClientViewModel? newValue)
    {
        if (!ShouldSync)
        {
            return;
        }

        if (newValue is not null)
        {
            if (_activeDialogs.TryGetValue(newValue.Id, out var dialog))
            {
                ActiveDialog = dialog;
            }
        }
        else
        {
            ActiveDialog = null;
        }
    }

    partial void OnActiveDialogChanged(DialogViewModel? oldValue, DialogViewModel? newValue)
    {
        if (oldValue is not null)
        {
            Unsubscribe(oldValue);
        }

        if (newValue is not null)
        {
            Subscribe(newValue);
        }
    }

    private void Subscribe(DialogViewModel dialog)
    {
        dialog.MenuChoiceSelected += OnDialogMenuChoiceSelected;
        dialog.TextInputConfirmed += OnTextInputConfirmed;
        dialog.RequestPrevious += OnDialogNavigatePrevious;
        dialog.RequestNext += OnDialogNavigateNext;
        dialog.RequestTop += OnDialogNavigateTop;
        dialog.RequestClose += OnDialogClose;
    }

    private void Unsubscribe(DialogViewModel dialog)
    {
        dialog.MenuChoiceSelected -= OnDialogMenuChoiceSelected;
        dialog.TextInputConfirmed -= OnTextInputConfirmed;
        dialog.RequestPrevious -= OnDialogNavigatePrevious;
        dialog.RequestNext -= OnDialogNavigateNext;
        dialog.RequestTop -= OnDialogNavigateTop;
        dialog.RequestClose -= OnDialogClose;
    }

    [RelayCommand]
    private async Task CopyDialogTextToClipboard()
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null || ActiveDialog is null)
        {
            return;
        }

        await clipboard.SetTextAsync(ActiveDialog.Content);
    }

    [RelayCommand]
    private void LoadDialog()
    {

    }

    [RelayCommand]
    private void SaveDialog()
    {

    }
}
