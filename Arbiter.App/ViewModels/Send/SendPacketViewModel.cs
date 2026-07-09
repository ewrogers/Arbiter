using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Arbiter.App.Services.Entities;
using Arbiter.App.ViewModels.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Send;

public partial class SendPacketViewModel : ViewModelBase
{
    private readonly ILogger<SendPacketViewModel> _logger;
    private readonly ClientManagerViewModel _clientManager;
    private readonly IEntityStore _entityStore;

    private string _inputText = string.Empty;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (!SetProperty(ref _inputText, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsEmpty));

            // Debounce validation when the input changes
            _validationDebouncer.Execute(PerformValidation);

            StartSendCommand.NotifyCanExecuteChanged();
            ClearAllCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(InputText);

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSendCommand))]
    private bool _hasClients;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSendCommand))]
    private bool _hasPackets;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSendCommand))]
    private bool _hasDisconnects;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSendCommand))]
    private string? _validationError;

    [ObservableProperty] private double? _validationErrorOpacity;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(StartSendCommand))]
    private ClientViewModel? _selectedClient;

    [ObservableProperty] private TimeSpan _selectedDelay = TimeSpan.Zero;
    [ObservableProperty] private TimeSpan _selectedRate = TimeSpan.FromMilliseconds(100);
    [ObservableProperty] private bool _loopEnabled;
    [ObservableProperty] private int _loopCount = 1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSendCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopSendCommand))]
    [NotifyCanExecuteChangedFor(nameof(CutSelectionCommand), nameof(CommentSelectionCommand),
        nameof(UncommentSelectionCommand))]
    private bool _isSending;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSendCommand), nameof(CopyToClipboardCommand), nameof(CutSelectionCommand),
        nameof(CommentSelectionCommand), nameof(UncommentSelectionCommand))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private int _selectionStart;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartSendCommand), nameof(CopyToClipboardCommand), nameof(CutSelectionCommand),
        nameof(CommentSelectionCommand), nameof(UncommentSelectionCommand))]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private int _selectionEnd;

    public bool HasSelection => Math.Abs(SelectionStart - SelectionEnd) != 0;

    public List<TimeSpan> AvailableDelays =>
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(30)
    ];

    public List<TimeSpan> AvailableRates =>
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300),
        TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(600),
        TimeSpan.FromMilliseconds(700), TimeSpan.FromMilliseconds(800), TimeSpan.FromMilliseconds(900),
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(5)
    ];

    public SendPacketViewModel(ILogger<SendPacketViewModel> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _clientManager = serviceProvider.GetRequiredService<ClientManagerViewModel>();
        _entityStore = serviceProvider.GetRequiredService<IEntityStore>();

        _clientManager.Clients.CollectionChanged += OnClientsCollectionChanged;
        _clientManager.ClientSelected += OnClientSelected;
    }

    partial void OnSelectedClientChanged(ClientViewModel? oldValue, ClientViewModel? newValue)
    {
        if (oldValue != newValue)
        {
            StopSend();
        }
    }

    private void OnClientSelected(ClientViewModel? client)
    {
        SelectedClient = client;
    }

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

        StopSend();
        SelectedClient = null;
    }

    private bool CanSend() =>
        !IsSending && (HasPackets || HasDisconnects) && SelectedClient is not null && HasValidInput();

    private bool HasValidInput()
    {
        // For immediate validation check (used by send command), parse without debouncing
        var lines = InputText.Split(NewLineCharacters, StringSplitOptions.RemoveEmptyEntries);
        return SendItemParser.TryParse(lines, out _, out _);
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    public void StartSend()
    {
        if (IsSending || SelectedClient is null)
        {
            return;
        }

        // Check if there is a valid selection and use that instead
        var selectionStart = Math.Min(SelectionStart, SelectionEnd);
        var selectionEnd = Math.Max(SelectionStart, SelectionEnd);
        var selectionLength = selectionEnd - selectionStart;

        var inputText = selectionLength > 0 ? InputText.Substring(selectionStart, selectionLength) : InputText;

        // Perform immediate validation before sending
        var lines = inputText.Split(NewLineCharacters, StringSplitOptions.RemoveEmptyEntries);
        if (!SendItemParser.TryParse(lines, out var items, out var validationError))
        {
            // Update UI to show the validation error immediately
            ValidationError = validationError;
            ValidationErrorOpacity = 1;
            StartSendCommand.NotifyCanExecuteChanged();
            return;
        }

        // Commit parsed items for sending
        _parsedItems.Clear();
        _parsedItems.AddRange(items);

        HasPackets = _parsedItems.Any(x => x.Packet is not null || x.Command.HasValue);
        HasDisconnects = _parsedItems.Any(x => x.IsDisconnect);

        IsSending = true;

        // Create a fresh CTS for each run
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _ = RunSendLoopAsync(_cancellationTokenSource.Token);
    }

    private bool CanStopSend() => IsSending;

    [RelayCommand(CanExecute = nameof(CanStopSend))]
    public void StopSend()
    {
        if (!IsSending)
        {
            return;
        }

        try
        {
            _cancellationTokenSource?.Cancel();
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            IsSending = false;
        }
    }
}
