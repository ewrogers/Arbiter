using Arbiter.App.Models.Tracing;
using Arbiter.App.Models.Tracing.Queries;

namespace Arbiter.App.Tests.Models.Tracing;

public sealed class TracePayloadFormatterTests
{
    [Test]
    public void Should_Format_Payload_As_Sixteen_Byte_Hex_And_Ascii_Lines()
    {
        var bytes = Enumerable.Range(0x41, 17).Select(value => (byte)value).ToArray();

        var result = TracePayloadFormatter.Format(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Hex,
                Is.EqualTo("41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 50"));
            Assert.That(result[0].Ascii, Is.EqualTo("ABCDEFGHIJKLMNOP"));
            Assert.That(result[1].Hex, Is.EqualTo("51"));
            Assert.That(result[1].Ascii, Is.EqualTo("Q"));
        });
    }

    [Test]
    public void Should_Split_Search_Highlights_Across_Payload_Lines()
    {
        var bytes = new byte[18];
        TraceQueryHighlight[] highlights =
        [
            new(14, 4, TraceQueryHighlightSource.Data)
        ];

        var result = TracePayloadFormatter.Format(bytes, highlights);

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Highlights,
                Is.EqualTo(new[] { new TraceQueryHighlight(14, 2, TraceQueryHighlightSource.Data) }));
            Assert.That(result[1].Highlights,
                Is.EqualTo(new[] { new TraceQueryHighlight(0, 2, TraceQueryHighlightSource.Data) }));
        });
    }

    [TestCase(650, 10, 16)]
    [TestCase(959, 10, 24)]
    [TestCase(1279, 10, 32)]
    [TestCase(1599, 10, 40)]
    public void Should_Use_The_Highest_Eight_Byte_Multiple_That_Fits(
        double availableWidth,
        double characterWidth,
        int expected)
    {
        Assert.That(
            TracePayloadFormatter.CalculateBytesPerLine(availableWidth, characterWidth),
            Is.EqualTo(expected));
    }

    [Test]
    public void Should_Format_With_The_Responsive_Bytes_Per_Line()
    {
        var bytes = Enumerable.Range(0, 25).Select(value => (byte)value).ToArray();

        var result = TracePayloadFormatter.Format(bytes, bytesPerLine: 24);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Ascii, Has.Length.EqualTo(24));
            Assert.That(result[1].Hex, Is.EqualTo("18"));
            Assert.That(TracePayloadFormatter.CalculateHexColumnWidth(24, 10), Is.EqualTo(710));
        });
    }

    [Test]
    public void Should_Return_No_Lines_For_Empty_Payload()
    {
        Assert.That(TracePayloadFormatter.Format([]), Is.Empty);
    }
}
