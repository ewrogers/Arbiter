using Arbiter.App.Controls;
using Arbiter.App.Models.Tracing.Queries;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Arbiter.App.Tests.Controls;

public sealed class HighlightedByteTextBlockTests
{
    [Test]
    public void Should_Render_Highlights_With_Foreground_Background_And_Border()
    {
        var block = new HighlightedHexTextBlock
        {
            SourceText = "01 02",
            Highlights = [new TraceQueryHighlight(0, 1, TraceQueryHighlightSource.Data)],
            HighlightForeground = Brushes.Yellow,
            HighlightBackground = Brushes.Olive,
            HighlightBorderBrush = Brushes.Yellow
        };

        var container = block.Inlines![0] as InlineUIContainer;
        var border = container?.Child as Border;
        var text = border?.Child as TextBlock;

        Assert.Multiple(() =>
        {
            Assert.That(container, Is.Not.Null);
            Assert.That(border?.BorderBrush, Is.SameAs(Brushes.Yellow));
            Assert.That(border?.Background, Is.SameAs(Brushes.Olive));
            Assert.That(text?.Foreground, Is.SameAs(Brushes.Yellow));
            Assert.That(text?.Text, Is.EqualTo("01"));
        });
    }
}
