using System;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Inspector;

public abstract partial class InspectorItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _order = int.MaxValue;
    [ObservableProperty] private bool _isCopyFeedbackActive;

    private int _copyFeedbackVersion;

    private bool CheckCanCopy(object _) => CanCopyToClipboard();
    
    [RelayCommand(CanExecute = nameof(CheckCanCopy))]
    private async Task CopyToClipboard(object _)
    {
        var textToCopy = GetCopyableValue();

        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is not null && textToCopy is not null)
        {
            await clipboard.SetTextAsync(textToCopy);
            ShowCopyFeedback();
        }
    }

    private void ShowCopyFeedback()
    {
        IsCopyFeedbackActive = true;
        var version = ++_copyFeedbackVersion;

        DispatcherTimer.RunOnce(() =>
        {
            if (version == _copyFeedbackVersion)
            {
                IsCopyFeedbackActive = false;
            }
        }, TimeSpan.FromMilliseconds(350));
    }

    protected abstract string? GetCopyableValue();

    protected virtual bool CanCopyToClipboard() => true;
}
