using System;
using System.Collections.Generic;
using System.Linq;
using Arbiter.App.Models.Tracing.Queries;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Arbiter.App.Controls;

public class HighlightedHexTextBlock : TextBlock
{
    public static readonly StyledProperty<string?> SourceTextProperty =
        AvaloniaProperty.Register<HighlightedHexTextBlock, string?>(nameof(SourceText));

    public static readonly StyledProperty<IReadOnlyList<TraceQueryHighlight>?> HighlightsProperty =
        AvaloniaProperty.Register<HighlightedHexTextBlock, IReadOnlyList<TraceQueryHighlight>?>(nameof(Highlights));

    public static readonly StyledProperty<IBrush?> HighlightBackgroundProperty =
        AvaloniaProperty.Register<HighlightedHexTextBlock, IBrush?>(nameof(HighlightBackground));

    public static readonly StyledProperty<IBrush?> HighlightForegroundProperty =
        AvaloniaProperty.Register<HighlightedHexTextBlock, IBrush?>(nameof(HighlightForeground));

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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SourceTextProperty ||
            change.Property == HighlightsProperty ||
            change.Property == HighlightBackgroundProperty ||
            change.Property == HighlightForegroundProperty)
        {
            RebuildInlines();
        }
    }

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

            Inlines?.Add(new Run(text.Substring(range.Start, range.Length))
            {
                Background = HighlightBackground,
                Foreground = HighlightForeground
            });
            position = range.Start + range.Length;
        }

        if (position < text.Length)
        {
            Inlines?.Add(new Run(text[position..]));
        }
    }

    private static IReadOnlyList<CharacterRange> GetCharacterRanges(
        string text,
        IReadOnlyList<TraceQueryHighlight>? highlights)
    {
        if (string.IsNullOrEmpty(text) || highlights is not { Count: > 0 })
        {
            return [];
        }

        var byteCount = (text.Length + 1) / 3;
        return highlights
            .Where(highlight => highlight.Offset >= 0 && highlight.Length > 0 && highlight.Offset < byteCount)
            .Select(highlight =>
            {
                var byteLength = Math.Min(highlight.Length, byteCount - highlight.Offset);
                var start = highlight.Offset * 3;
                var length = Math.Min(byteLength * 3 - 1, text.Length - start);
                return new CharacterRange(start, length);
            })
            .Where(range => range.Length > 0)
            .ToList();
    }

    private readonly record struct CharacterRange(int Start, int Length);
}
