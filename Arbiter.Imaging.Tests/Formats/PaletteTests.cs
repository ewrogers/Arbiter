using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Tests.Formats;

public sealed class PaletteTests
{
    [Test]
    public void Should_Treat_Palette_Index_Zero_As_Transparent()
    {
        var palette = Palette.Parse(TestImageData.Palette((0, 255, 255, 255)));
        Span<byte> rgba = stackalloc byte[4];

        palette.GetColor(0, false, rgba);

        Assert.That(rgba.ToArray(), Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
    }

    [Test]
    public void Should_Reject_Palettes_With_The_Wrong_Size()
    {
        Assert.That(() => Palette.Parse(new byte[10]), Throws.TypeOf<InvalidDataException>());
    }
}
