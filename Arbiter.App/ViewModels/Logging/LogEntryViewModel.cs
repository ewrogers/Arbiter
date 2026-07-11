using System;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Arbiter.App.Logging;
using Avalonia;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Logging;

public partial class LogEntryViewModel(ArbiterLogEntry logEntry) : ViewModelBase
{
    public DateTime Timestamp { get; } = logEntry.Timestamp;
    public string Category { get; } = logEntry.Category;
    public LogLevel Level { get; } = logEntry.Level;
    public string Message { get; } = logEntry.Message;
    public Exception? Exception { get; } = logEntry.Exception;

    public string FormattedLevel => Level switch
    {
        LogLevel.Critical or LogLevel.Error => "ERROR",
        LogLevel.Warning => "WARN",
        LogLevel.Information => "INFO",
        LogLevel.Debug or LogLevel.Trace => "DEBUG",
        _ => "????"
    };

    public string FormattedMessage => Message;

    public string FormattedException =>
        Exception is null ? string.Empty : $"{Exception.GetType().Name}: {Exception.Message}";

    public string FormattedStackTrace =>
        Exception?.StackTrace is null ? string.Empty : $"Stack Trace:{Environment.NewLine}{Exception.StackTrace}";
    
    public string CopyMenuText => string.IsNullOrEmpty(FormattedException) ? "Copy Message" : "Copy Exception";

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        var textToCopy = GetCopyableValue();

        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is not null)
        {
            await clipboard.SetTextAsync(textToCopy);
        }
    }

    private string GetCopyableValue() => string.IsNullOrEmpty(FormattedException)
        ? FormattedMessage
        : $"{FormattedException}{Environment.NewLine}{FormattedStackTrace}";
}
