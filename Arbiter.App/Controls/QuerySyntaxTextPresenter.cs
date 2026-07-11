using System;
using System.Collections.Generic;
using Arbiter.App.Models.Tracing.Queries;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Arbiter.App.Controls;

public sealed class QuerySyntax
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<QuerySyntax, TextBox, bool>("IsEnabled");

    public static bool GetIsEnabled(TextBox textBox) => textBox.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(TextBox textBox, bool value) => textBox.SetValue(IsEnabledProperty, value);
}

public class QuerySyntaxTextPresenter : TextPresenter
{
    public static readonly StyledProperty<bool> IsQuerySyntaxEnabledProperty =
        AvaloniaProperty.Register<QuerySyntaxTextPresenter, bool>(nameof(IsQuerySyntaxEnabled));

    public static readonly StyledProperty<IBrush?> KeywordBrushProperty =
        AvaloniaProperty.Register<QuerySyntaxTextPresenter, IBrush?>(nameof(KeywordBrush));

    public static readonly StyledProperty<IBrush?> GroupingBrushProperty =
        AvaloniaProperty.Register<QuerySyntaxTextPresenter, IBrush?>(nameof(GroupingBrush));

    public bool IsQuerySyntaxEnabled
    {
        get => GetValue(IsQuerySyntaxEnabledProperty);
        set => SetValue(IsQuerySyntaxEnabledProperty, value);
    }

    public IBrush? KeywordBrush
    {
        get => GetValue(KeywordBrushProperty);
        set => SetValue(KeywordBrushProperty, value);
    }

    public IBrush? GroupingBrush
    {
        get => GetValue(GroupingBrushProperty);
        set => SetValue(GroupingBrushProperty, value);
    }

    protected override TextLayout CreateTextLayout()
    {
        if (!IsQuerySyntaxEnabled || KeywordBrush is null || !string.IsNullOrEmpty(PreeditText) ||
            PasswordChar != default && !RevealPassword)
        {
            return base.CreateTextLayout();
        }

        var text = Text ?? string.Empty;
        var textBox = TemplatedParent as TextBox;
        var fontFamily = textBox?.FontFamily ?? FontFamily;
        var fontSize = textBox?.FontSize ?? FontSize;
        var fontStyle = textBox?.FontStyle ?? FontStyle;
        var fontWeight = textBox?.FontWeight ?? FontWeight;
        var fontStretch = textBox?.FontStretch ?? FontStretch;
        var foreground = textBox?.Foreground ?? Foreground;
        var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
        var overrides = new List<ValueSpan<TextRunProperties>>();

        foreach (var span in TraceQuerySyntax.GetSpans(text))
        {
            var brush = span.Kind == TraceQuerySyntaxKind.Grouping ? GroupingBrush : KeywordBrush;
            if (brush is null)
            {
                continue;
            }

            overrides.Add(new ValueSpan<TextRunProperties>(
                span.Start,
                span.Length,
                new GenericTextRunProperties(
                    typeface,
                    fontSize,
                    foregroundBrush: brush,
                    fontFeatures: FontFeatures)));
        }

        var selectionStart = Math.Min(SelectionStart, SelectionEnd);
        var selectionLength = Math.Abs(SelectionEnd - SelectionStart);
        if (ShowSelectionHighlight && selectionLength > 0 && SelectionForegroundBrush is not null)
        {
            overrides.Add(new ValueSpan<TextRunProperties>(
                selectionStart,
                selectionLength,
                new GenericTextRunProperties(
                    typeface,
                    fontSize,
                    foregroundBrush: SelectionForegroundBrush,
                    fontFeatures: FontFeatures)));
        }

        return new TextLayout(
            text,
            typeface,
            fontSize,
            foreground,
            TextAlignment,
            TextWrapping,
            maxWidth: double.PositiveInfinity,
            maxHeight: double.PositiveInfinity,
            textStyleOverrides: overrides,
            flowDirection: FlowDirection,
            lineHeight: LineHeight,
            letterSpacing: LetterSpacing,
            fontFeatures: FontFeatures);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsQuerySyntaxEnabledProperty ||
            change.Property == KeywordBrushProperty ||
            change.Property == GroupingBrushProperty)
        {
            InvalidateTextLayout();
        }
    }
}
