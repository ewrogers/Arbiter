using Arbiter.App.Controls;
using Arbiter.App.Models.Tracing.Queries;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Arbiter.App.Tests.Controls;

public sealed class HighlightedByteTextBlockTests
{
    [Test]
    public void Should_Render_Highlights_As_Seamless_Foreground_And_Background_Runs()
    {
        var block = new HighlightedHexTextBlock
        {
            SourceText = "01 02",
            Highlights = [new TraceQueryHighlight(0, 1, TraceQueryHighlightSource.Data)],
            HighlightForeground = Brushes.Yellow,
            HighlightBackground = Brushes.Olive
        };

        var run = block.Inlines![0] as Run;

        Assert.Multiple(() =>
        {
            Assert.That(run, Is.Not.Null);
            Assert.That(run?.Background, Is.SameAs(Brushes.Olive));
            Assert.That(run?.Foreground, Is.SameAs(Brushes.Yellow));
            Assert.That(run?.Text, Is.EqualTo("01"));
        });
    }
}
