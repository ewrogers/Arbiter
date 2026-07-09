using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Arbiter.App.Collections;
using Arbiter.App.Logging;
using Arbiter.App.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Logging;

public partial class ConsoleViewModel : ViewModelBase
{
    private sealed record QueuedLogEntry(long Generation, ArbiterLogEntry Entry);

    private readonly ObservableCollection<LogEntryViewModel> _allLogEntries = [];
    private readonly DispatcherBatchQueue<QueuedLogEntry> _logQueue;

    private long _logGeneration;

    public FilteredObservableCollection<LogEntryViewModel> FilteredLogEntries { get; }

    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private int _debugCount;
    [ObservableProperty] private int _infoCount;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private int _errorCount;

    [ObservableProperty] private bool _scrollToEndRequested;

    private bool _showDebugMessages = true;
    private bool _showInfoMessages = true;
    private bool _showWarningMessages = true;
    private bool _showErrorMessages = true;

    public bool ShowDebugMessages
    {
        get => _showDebugMessages;
        set
        {
            if (SetProperty(ref _showDebugMessages, value))
            {
                FilteredLogEntries.Refresh();
                OnPropertyChanged(nameof(FilteredLogEntries));
            }
        }
    }

    public bool ShowInfoMessages
    {
        get => _showInfoMessages;
        set
        {
            if (SetProperty(ref _showInfoMessages, value))
            {
                FilteredLogEntries.Refresh();
                OnPropertyChanged(nameof(FilteredLogEntries));
            }
        }
    }

    public bool ShowWarningMessages
    {
        get => _showWarningMessages;
        set
        {
            if (SetProperty(ref _showWarningMessages, value))
            {
                FilteredLogEntries.Refresh();
                OnPropertyChanged(nameof(FilteredLogEntries));
            }
        }
    }

    public bool ShowErrorMessages
    {
        get => _showErrorMessages;
        set
        {
            if (SetProperty(ref _showErrorMessages, value))
            {
                FilteredLogEntries.Refresh();
                OnPropertyChanged(nameof(FilteredLogEntries));
            }
        }
    }

    public ConsoleViewModel(ArbiterLoggerProvider provider)
    {
        FilteredLogEntries = new FilteredObservableCollection<LogEntryViewModel>(_allLogEntries, MatchesFilter);
        _logQueue = new DispatcherBatchQueue<QueuedLogEntry>(ApplyLogBatch);

        provider.LogEntryCreated += OnLogEntryCreated;
    }

    private void OnLogEntryCreated(ArbiterLogEntry entry)
    {
        var generation = Volatile.Read(ref _logGeneration);
        _logQueue.Enqueue(new QueuedLogEntry(generation, entry));
    }

    private void ApplyLogBatch(IReadOnlyList<QueuedLogEntry> entries)
    {
        var generation = Volatile.Read(ref _logGeneration);
        foreach (var queued in entries)
        {
            if (queued.Generation != generation)
            {
                continue;
            }

            var entry = new LogEntryViewModel(queued.Entry);
            _allLogEntries.Add(entry);
            IncrementCount(entry.Level);
        }

        IsEmpty = _allLogEntries.Count == 0;
    }

    private void IncrementCount(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Debug or LogLevel.Trace:
                DebugCount++;
                break;
            case LogLevel.Information:
                InfoCount++;
                break;
            case LogLevel.Warning:
                WarningCount++;
                break;
            case LogLevel.Error or LogLevel.Critical:
                ErrorCount++;
                break;
        }
    }

    private bool MatchesFilter(LogEntryViewModel logEntry)
    {
        return logEntry.Level switch
        {
            LogLevel.Error or LogLevel.Critical => ShowErrorMessages,
            LogLevel.Warning => ShowWarningMessages,
            LogLevel.Information => ShowInfoMessages,
            LogLevel.Debug or LogLevel.Trace => ShowDebugMessages,
            _ => true
        };
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logQueue.Clear();
        Interlocked.Increment(ref _logGeneration);

        _allLogEntries.Clear();
        IsEmpty = true;
        DebugCount = 0;
        InfoCount = 0;
        WarningCount = 0;
        ErrorCount = 0;
    }

    [RelayCommand]
    private void ScrollToEnd()
    {
        ScrollToEndRequested = true;
    }
}
