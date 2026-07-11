using System;
using Arbiter.App.Models.Tracing;
using Arbiter.App.ViewModels.Tracing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Arbiter.App.Views;

public partial class TracePacketView : UserControl
{
    public TracePacketView()
    {
        InitializeComponent();
        DetailedPayloadList.SizeChanged += OnDetailedPayloadListSizeChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateDetailedLayout(DetailedPayloadList.Bounds.Width);
    }

    private void OnDetailedPayloadListSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateDetailedLayout(e.NewSize.Width);
    }

    private void UpdateDetailedLayout(double availableWidth)
    {
        if (availableWidth <= 0 || DataContext is not TracePacketViewModel viewModel)
        {
            return;
        }

        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var layout = new TextLayout(
            "0",
            typeface,
            FontSize,
            Brushes.Transparent,
            TextAlignment.Left,
            TextWrapping.NoWrap,
            maxWidth: double.PositiveInfinity,
            maxHeight: double.PositiveInfinity);
        var characterWidth = layout.WidthIncludingTrailingWhitespace;
        var bytesPerLine = TracePayloadFormatter.CalculateBytesPerLine(availableWidth, characterWidth);

        viewModel.DetailedBytesPerLine = bytesPerLine;
        viewModel.DetailedHexColumnWidth =
            TracePayloadFormatter.CalculateHexColumnWidth(bytesPerLine, characterWidth);
    }
}
