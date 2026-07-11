using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace Arbiter.App.Extensions;

public static class ApplicationExtensions
{
    public static IClipboard? TryGetClipboard(this Application app)
    {
        if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        if (app.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var topLevel = TopLevel.GetTopLevel(singleView.MainView);
            if (topLevel is not null)
            {
                return topLevel.Clipboard;
            }
        }

        return null;
    }
}
