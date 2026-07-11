using System.Collections.Generic;
using Arbiter.App.Models.Tracing.Queries;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Arbiter.App.Controls;

public abstract class HighlightedByteTextBlock : TextBlock
{
    public static readonly StyledProperty<string?> SourceTextProperty =
        AvaloniaProperty.Register<HighlightedByteTextBlock, string?>(nameof(SourceText));

    public static readonly StyledProperty<IReadOnlyList<TraceQueryHighlight>?> HighlightsProperty =
        AvaloniaProperty.Register<HighlightedByteTextBlock, IReadOnlyList<TraceQueryHighlight>?>(nameof(Highlights));

    public static readonly StyledProperty<IBrush?> HighlightBackgroundProperty =
        AvaloniaProperty.Register<HighlightedByteTextBlock, IBrush?>(nameof(HighlightBackground));

    public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
        AvaloniaProperty.Register<HighlightedByteTextBlock, IBrush?>(nameof(HighlightForeground));

    public static readonly StyledProperty<IBrush?> HighlightBorderBrushProperty =
        AvaloniaProperty.Register<HighlightedByteTextBlock, IBrush?>(nameof(HighlightBorderBrush));

    public string? SourceText
    {
        get => GetValue(SourceTextProperty);
        set => SetValue(SourceTextProperty, value);
    }

    public IReadOnlyList<TraceQueryHighlight>? Highlights
    {
        get => GetValue(HighlightsProperty);
        set => SetValue(HighlightsProperty, value);
    }

    public IBrush? HighlightBackground
    {
        get => GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    public IBrush? HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public IBrush? HighlightBorderBrush
    {
        get => GetValue(HighlightBorderBrushProperty);
        set => SetValue(HighlightBorderBrushProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceTextProperty ||
            change.Property == HighlightsProperty ||
            change.Property == HighlightBackgroundProperty ||
            change.Property == HighlightForegroundProperty ||
            change.Property == HighlightBorderBrushProperty ||
            change.Property == FontFamilyProperty ||
            change.Property == FontSizeProperty ||
            change.Property == FontStyleProperty ||
            change.Property == FontWeightProperty ||
            change.Property == FontStretchProperty ||
            change.Property == LetterSpacingProperty ||
            change.Property == LineHeightProperty)
        {
            RebuildInlines();
        }
    }

    protected abstract CharacterRange GetCharacterRange(string text, int byteOffset, int byteLength);

    protected abstract int GetByteCount(string text);

    private void RebuildInlines()
    {
        var text = SourceText ?? string.Empty;
        Inlines?.Clear();

        var ranges = GetCharacterRanges(text, Highlights);
        if (ranges.Count == 0)
        {
            Inlines?.Add(new Run(text));
            return;
        }

        var position = 0;
        foreach (var range in ranges)
        {
            if (range.Start > position)
            {
                Inlines?.Add(new Run(text[position..range.Start]));
            }

            Inlines?.Add(CreateHighlightInline(text.Substring(range.Start, range.Length)));
            position = range.Start + range.Length;
        }

        if (position < text.Length)
        {
            Inlines?.Add(new Run(text[position..]));
        }
    }

    private InlineUIContainer CreateHighlightInline(string text)
    {
        var highlightedText = new TextBlock
        {
            Text = text,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontStyle = FontStyle,
            FontWeight = FontWeight,
            FontStretch = FontStretch,
            LetterSpacing = LetterSpacing,
            LineHeight = LineHeight,
            Foreground = HighlightForeground,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var border = new Border
        {
            Background = HighlightBackground,
            BorderBrush = HighlightBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(1, 0),
            Child = highlightedText,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        return new InlineUIContainer(border)
        {
            BaselineAlignment = BaselineAlignment.Center
        };
    }

    private IReadOnlyList<CharacterRange> GetCharacterRanges(
        string text,
        IReadOnlyList<TraceQueryHighlight>? highlights)
    {
        if (string.IsNullOrEmpty(text) || highlights is not { Count: > 0 })
        {
            return [];
        }

        var byteCount = GetByteCount(text);
        var ranges = new List<CharacterRange>();
        foreach (var highlight in highlights)
        {
            if (highlight.Offset < 0 || highlight.Length <= 0 || highlight.Offset >= byteCount)
            {
                continue;
            }

            var byteLength = System.Math.Min(highlight.Length, byteCount - highlight.Offset);
            var range = GetCharacterRange(text, highlight.Offset, byteLength);
            if (range.Length > 0)
            {
                ranges.Add(range);
            }
        }

        return ranges;
    }

    protected readonly record struct CharacterRange(int Start, int Length);
}
