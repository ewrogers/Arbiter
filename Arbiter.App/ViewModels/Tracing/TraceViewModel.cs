using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbiter.App.Collections;
using Arbiter.App.Models;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Services.Dialogs;
using Arbiter.App.Services.Input;
using Arbiter.App.Services.Tracing;
using Arbiter.App.Threading;
using Arbiter.Net.Proxy;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceViewModel : ViewModelBase
{
    private static readonly string TracesDirectory = AppHelper.GetRelativePath("traces");

    private static readonly FilePickerFileType JsonFileType = new("JSON Files")
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"],
    };

    private readonly ILogger<TraceViewModel> _logger;
    private readonly IStorageProvider _storageProvider;
    private readonly IKeyboardService _keyboardService;
    private readonly IDialogService _dialogService;
    private readonly ITraceService _traceService;
    private readonly ProxyServer _proxyServer;

    private readonly ObservableCollection<TracePacketViewModel> _allPackets = [];

    private long _indexCounter = 1;
    private bool _isEmpty = true;
    private PacketDisplayMode _packetDisplayMode = PacketDisplayMode.Decrypted;

    [ObservableProperty] private int _maxTraceHistory;
    [ObservableProperty] private DateTime _startTime;
    [ObservableProperty] private bool _scrollToEndRequested;
    [ObservableProperty] private int? _scrollToIndexRequested;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isLive;
    [ObservableProperty] private bool _isDirty;

    [ObservableProperty] private TraceClientViewModel? _selectedTraceClient;

    [ObservableProperty] private string? _traceClientName;

    public ObservableCollection<TraceClientViewModel> TraceClients { get; } = [new("All Clients")];

    public bool IsEmpty => _isEmpty;

    public bool ShowRawPackets
    {
        get => _packetDisplayMode == PacketDisplayMode.Raw;
        set
        {
            var newValue = value ? PacketDisplayMode.Raw : PacketDisplayMode.Decrypted;
            if (!SetProperty(ref _packetDisplayMode, newValue))
            {
                return;
            }

            OnPropertyChanged();
            foreach (var packet in _allPackets)
            {
                packet.DisplayMode = newValue;
            }
        }
    }

    public TraceViewModel(ILogger<TraceViewModel> logger, IStorageProvider storageProvider,
        IKeyboardService keyboardService,
        IDialogService dialogService, ITraceService traceService, ProxyServer proxyServer)
    {
        _logger = logger;
        _storageProvider = storageProvider;
        _keyboardService = keyboardService;
        _dialogService = dialogService;
        _traceService = traceService;
        _proxyServer = proxyServer;

        _proxyServer.ClientAuthenticated += OnClientAuthenticated;
        _proxyServer.ClientDisconnected += OnClientDisconnected;

        SelectedTraceClient = TraceClients.FirstOrDefault();
        FilteredPackets = new FilteredObservableCollection<TracePacketViewModel>(_allPackets, MatchesFilter);
        _packetQueue = new DispatcherBatchQueue<QueuedTracePacket>(ApplyPacketBatch);

        _allPackets.CollectionChanged += OnPacketCollectionChanged;
        SelectedPackets.CollectionChanged += OnSelectedPacketsCollectionChanged;

        FilterParameters.PropertyChanged += OnFilterParametersChanged;
        FilterParameters.Clients.CollectionChanged += OnFilterClientsCollectionChanged;
        SearchParameters.PropertyChanged += OnSearchParametersChanged;
    }

    private void OnClientAuthenticated(object? sender, ProxyConnectionEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Connection.Name))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var client = new TraceClientViewModel(e.Connection.Name, e.Connection.Name);
            TraceClients.Add(client);

            // Re-select the client if it was previously selected (can happen after redirect)
            if (string.Equals(client.Name, TraceClientName, StringComparison.OrdinalIgnoreCase))
            {
                SelectedTraceClient = client;
            }

        }, DispatcherPriority.Background);
    }

    private void OnClientDisconnected(object? sender, ProxyConnectionEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Connection.Name))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            PruneClients();

            // If not running, select "all clients"
            if (!IsRunning)
            {
                SelectedTraceClient = TraceClients.FirstOrDefault();
            }
        }, DispatcherPriority.Background);
    }

    private void OnPacketCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var collection = (ObservableCollection<TracePacketViewModel>)sender!;
        if (SetProperty(ref _isEmpty, collection.Count == 0))
        {
            OnPropertyChanged(nameof(IsEmpty));
        }

        // When packets are removed/reset, prune client filters that no longer exist in any remaining packet
        if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Reset)
        {
            PruneClientsNotInPackets();
        }
    }

    private void OnPacketReceived(object? sender, ProxyConnectionDataEventArgs e)
    {
        QueuePacket(e);
    }

    private void OnPacketQueued(object? sender, ProxyConnectionDataEventArgs e)
    {
        QueuePacket(e);
    }

    private void AddPacketToTrace(TracePacketViewModel vm, bool pruneHistory = true)
    {
        // Set the index before adding to the collection so that the index is correct when the collection is sorted
        var nextIndex = Interlocked.Increment(ref _indexCounter);
        vm.Index = nextIndex;

        var matchesSearch = MatchesSearch(vm);
        if (matchesSearch)
        {
            AddSearchResultIndex(_allPackets.Count);
        }

        vm.Opacity = matchesSearch ? 1 : 0.5;

        _allPackets.Add(vm);
        FilterParameters.TryAddClient(vm.ClientName ?? string.Empty);
        IsDirty = true;

        while (pruneHistory && _allPackets.Count > MaxTraceHistory)
        {
            _allPackets.RemoveAt(0);
        }
    }

    private void ClearPackets()
    {
        _packetQueue.Clear();
        Interlocked.Increment(ref _packetGeneration);

        _allPackets.Clear();
        SelectedPackets.Clear();

        FilterParameters.ClearClients();

        Interlocked.Exchange(ref _indexCounter, 1);

        IsDirty = false;
        OnPropertyChanged(nameof(FilteredPackets));
    }

    [RelayCommand]
    public void StartTracing()
    {
        if (IsRunning)
        {
            return;
        }

        TraceClientName = SelectedTraceClient?.Name;

        Volatile.Write(ref _isAcceptingPackets, 1);
        _proxyServer.PacketReceived += OnPacketReceived;
        _proxyServer.PacketQueued += OnPacketQueued;

        StartTime = DateTime.Now;
        IsRunning = true;
        IsLive = true;

        _logger.LogInformation("Trace started");
    }

    [RelayCommand]
    public void StopTracing()
    {
        if (!IsRunning)
        {
            return;
        }

        Volatile.Write(ref _isAcceptingPackets, 0);
        _proxyServer.PacketReceived -= OnPacketReceived;
        _proxyServer.PacketQueued -= OnPacketQueued;

        _packetQueue.DrainAll();
        Interlocked.Increment(ref _packetGeneration);

        IsRunning = false;

        PruneClients();
        _logger.LogInformation("Trace stopped");
    }

    private bool CanClearTrace() => !IsSavingTrace && !IsLoadingTrace;

    [RelayCommand(CanExecute = nameof(CanClearTrace))]
    private async Task ClearTrace()
    {
        var confirm = await _dialogService.ShowMessageBoxAsync(new MessageBoxDetails
        {
            Title = "Confirm Clear Trace",
            Message = "Are you sure you want to clear?\nThis will remove all packets from the trace.",
            Description = "This action cannot be undone.",
            Style = MessageBoxStyle.YesNo
        });

        if (confirm is not true)
        {
            return;
        }

        ClearPackets();
        _logger.LogInformation("Trace cleared");
    }

    [RelayCommand]
    private void ScrollToEnd()
    {
        ScrollToEndRequested = true;
    }

    private void PruneClients()
    {
        var liveClients = _proxyServer.Connections.Where(c => c.IsConnected).Select(c => c.Name).ToList();
        var deadClients = TraceClients
            .Where(c => !string.IsNullOrWhiteSpace(c.Name) &&
                        liveClients.All(n => !string.Equals(c.Name, n, StringComparison.OrdinalIgnoreCase))).ToList();

        foreach (var client in deadClients)
        {
            TraceClients.Remove(client);

            if (client == SelectedTraceClient)
            {
                TraceClientName = null;
                SelectedTraceClient = TraceClients.FirstOrDefault();
            }
        }

        SelectedTraceClient ??= TraceClients.FirstOrDefault();
    }
}
