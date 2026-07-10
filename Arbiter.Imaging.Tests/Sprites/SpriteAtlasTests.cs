using Arbiter.IO.Assets;
using Arbiter.Imaging.Sprites;

namespace Arbiter.Imaging.Tests.Sprites;

public sealed class SpriteAtlasTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "Arbiter.Imaging.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, true);
    }

    [Test]
    public void Should_Resolve_One_Based_Icons_When_The_Direct_Frame_Is_Empty()
    {
        TestImageData.WriteDat(
            _directory,
            "mock.dat",
            ("skill001.epf", TestImageData.Epf(1, 1, [1], null)),
            ("gui06.pal", TestImageData.Palette((1, 100, 150, 200))));
        var atlas = SpriteAtlasLoader.LoadNamed(
            DatAssetCatalog.Load(_directory),
            SpriteAtlasLoader.DefaultSkillImageName,
            SpriteAtlasLoader.DefaultGuiPaletteName);

        var found = atlas.TryResolveIcon(1, out var frameIndex, out var region);

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(frameIndex, Is.Zero);
            Assert.That(region, Is.EqualTo(new SpriteAtlasRegion(0, 0, 1, 1)));
        });
    }

    [Test]
    public void Should_Apply_Grayscale_And_Blue_Tint_Without_Changing_Alpha()
    {
        TestImageData.WriteDat(
            _directory,
            "mock.dat",
            ("skill001.epf", TestImageData.Epf(1, 1, [1])),
            ("gui06.pal", TestImageData.Palette((1, 100, 150, 200))));
        var atlas = SpriteAtlasLoader.LoadNamed(
            DatAssetCatalog.Load(_directory),
            SpriteAtlasLoader.DefaultSkillImageName,
            SpriteAtlasLoader.DefaultGuiPaletteName);

        var grayscale = SpriteAtlasTransforms.Grayscale(atlas);
        var tinted = SpriteAtlasTransforms.Tint(grayscale, 147, 189, 255);

        Assert.Multiple(() =>
        {
            Assert.That(grayscale.Pixels.ToArray(), Is.EqualTo(new byte[] { 141, 141, 141, 255 }));
            Assert.That(tinted.Pixels.ToArray(), Is.EqualTo(new byte[] { 81, 105, 141, 255 }));
        });
    }
}
