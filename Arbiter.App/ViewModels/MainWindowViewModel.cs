using System;
using System.Net;
using System.Threading.Tasks;
using Arbiter.App.Models;
using Arbiter.App.Models.Settings;
using Arbiter.App.Services.Client;
using Arbiter.App.Services.Dialogs;
using Arbiter.App.Services.Settings;
using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Client;
using Arbiter.App.ViewModels.Dialogs;
using Arbiter.App.ViewModels.Entities;
using Arbiter.App.ViewModels.Inspector;
using Arbiter.App.ViewModels.Logging;
using Arbiter.App.ViewModels.Proxy;
using Arbiter.App.ViewModels.Send;
using Arbiter.App.ViewModels.Tracing;
using Arbiter.App.Views;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ArbiterSettings Settings { get; set; } = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IDialogService _dialogService;
    private readonly IGameClientService _gameClientService;
    private readonly ISettingsService _settingsService;
    private readonly IGameSpriteService _gameSpriteService;
    private readonly Window _mainWindow;

    [ObservableProperty] private string _title = "Arbiter";
    [ObservableProperty] private RawHexViewModel? _selectedRawHex;

    public ClientManagerViewModel ClientManager { get; }
    public SendPacketViewModel SendPacket { get; }
    public ConsoleViewModel Console { get; }
    public InspectorViewModel Inspector { get; }
    public EntityManagerViewModel EntityManager { get; }
    public CrcCalculatorViewModel CrcCalculator { get; }
    public ProxyViewModel Proxy { get; }
    public TraceViewModel Trace { get; }
    public DialogManagerViewModel DialogManager { get; }

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        IServiceProvider serviceProvider,
        Window mainWindow)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        _dialogService = serviceProvider.GetRequiredService<IDialogService>();
        _gameClientService = serviceProvider.GetRequiredService<IGameClientService>();
        _settingsService = serviceProvider.GetRequiredService<ISettingsService>();
        _gameSpriteService = serviceProvider.GetRequiredService<IGameSpriteService>();
        _mainWindow = mainWindow;

        ClientManager = serviceProvider.GetRequiredService<ClientManagerViewModel>();
        SendPacket = serviceProvider.GetRequiredService<SendPacketViewModel>();
        Console = serviceProvider.GetRequiredService<ConsoleViewModel>();
        Inspector = serviceProvider.GetRequiredService<InspectorViewModel>();
        EntityManager = serviceProvider.GetRequiredService<EntityManagerViewModel>();
        CrcCalculator = serviceProvider.GetRequiredService<CrcCalculatorViewModel>();
        Proxy = serviceProvider.GetRequiredService<ProxyViewModel>();
        Trace = serviceProvider.GetRequiredService<TraceViewModel>();
        DialogManager = serviceProvider.GetRequiredService<DialogManagerViewModel>();

        Trace.SelectedPacketChanged += OnPacketSelected;
        ClientManager.ClientSelected += OnClientSelected;
    }

    private void OnClientSelected(ClientViewModel? selectedClient)
    {
        Title = selectedClient is null ? "Arbiter" : $"Arbiter - {selectedClient.Name}";
    }

    private bool CanLaunchClient() =>
        !string.IsNullOrWhiteSpace(Settings.ClientExecutablePath) && OperatingSystem.IsWindows();

    [RelayCommand(CanExecute = nameof(CanLaunchClient))]
    private async Task LaunchClient()
    {
        try
        {
            var clientExecutablePath = Settings.ClientExecutablePath;
            var options = new LaunchClientOptions(Settings.LocalPort, Settings.SkipIntroVideo,
                Settings.SuppressLoginNotice, Settings.ApplyModifiersKeyFix,
                Settings.AllowAltToShowGroundItems, Settings.SkipQuantityPromptInExchange,
                Settings.ShowItemQuantityInDialogs, Settings.MakeExchangeDialogDraggable,
                Settings.ShowExchangeResultsInMessageBar, Settings.ImprovedAutoFollow);

            await _gameClientService.LaunchLoopbackClient(clientExecutablePath, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch client");
            await _dialogService.ShowMessageBoxAsync(new MessageBoxDetails
            {
                Title = "Failed to Launch Client",
                Message = $"An error occurred while launching the client:\n\n{ex.Message}",
                Description = "You can change the client executable path in Settings."
            });
        }
    }

    private async Task StartProxyAsync()
    {
        try
        {
            var remoteIpAddress = await Dns.GetHostAddressesAsync(Settings.RemoteServerAddress);
            if (remoteIpAddress.Length == 0)
            {
                throw new Exception("Failed to resolve remote server address");
            }

            Proxy.Start(Settings.LocalPort, remoteIpAddress[0], Settings.RemoteServerPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start proxy server");
            await _dialogService.ShowMessageBoxAsync(new MessageBoxDetails
            {
                Title = "Failed to Start Proxy Server",
                Message = $"An error occurred while starting the proxy server:\n\n{ex.Message}",
                Description = "You can change the local and remote server in Settings."
            });
        }
    }

    private async Task UpdateRemoteEndpointAsync()
    {
        try
        {
            var remoteIpAddress = await Dns.GetHostAddressesAsync(Settings.RemoteServerAddress);
            if (remoteIpAddress.Length == 0)
            {
                throw new Exception("Failed to resolve remote server address");
            }

            Proxy.SetRemoteEndpoint(remoteIpAddress[0], Settings.RemoteServerPort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update remote server");
            await _dialogService.ShowMessageBoxAsync(new MessageBoxDetails
            {
                Title = "Failed to Update Remote Server",
                Message = $"An error occurred while updating the remote server:\n\n{ex.Message}",
                Description = "Check the remote server address and port in Settings."
            });
        }
    }

    private void OnPacketSelected(TracePacketViewModel? viewModel)
    {
        if (viewModel is null)
        {
            SelectedRawHex = null;
            Inspector.SelectedPacket = null;
            return;
        }

        SelectedRawHex = new RawHexViewModel(viewModel);
        SelectedRawHex.ClearSelection();

        Inspector.SelectedPacket = viewModel;
    }

    [RelayCommand]
    private async Task ShowSettings()
    {
        // Create the view model and set the selected tab index
        var vm = _serviceProvider.GetRequiredService<SettingsViewModel>(); 
        vm.SelectedTabIndex = Settings.SettingsPanelIndex;
        
        var newSettings =
            await _dialogService.ShowDialogAsync<SettingsWindow, SettingsViewModel, ArbiterSettings>(vm);
        
        if (newSettings is null)
        {
            Settings.SettingsPanelIndex = vm.SelectedTabIndex;
            return;
        }

        var remoteEndpointChanged =
            !string.Equals(Settings.RemoteServerAddress, newSettings.RemoteServerAddress,
                StringComparison.OrdinalIgnoreCase) ||
            Settings.RemoteServerPort != newSettings.RemoteServerPort;

        Settings = newSettings;
        Settings.SettingsPanelIndex = vm.SelectedTabIndex;
        
        await _settingsService.SaveToFileAsync(Settings);

        if (remoteEndpointChanged)
        {
            await UpdateRemoteEndpointAsync();
        }

        await _gameSpriteService.LoadAsync(Settings.ClientExecutablePath);
        LaunchClientCommand.NotifyCanExecuteChanged();

        ApplySettings();
    }

    private void ApplySettings(bool applyTraceDefaults = false)
    {
        Proxy.ApplyDebugFilters(Settings.Debug, Settings.MessageFilters);
        ClientManager.ApplySettings(Settings.Debug);
        
        Trace.MaxTraceHistory = Settings.TraceMaxHistory;
        if (applyTraceDefaults)
        {
            Trace.IsDetailedView = Settings.TraceDetailedView;
        }

        Trace.ConfigureDefaultCommandFilters(
            Settings.TraceDefaultClientCommands,
            Settings.TraceDefaultServerCommands,
            applyTraceDefaults);
        EntityManager.SortOrder = Settings.EntitySorting;
    }
}
