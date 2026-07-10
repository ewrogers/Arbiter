using Arbiter.IO.Assets;
using Arbiter.Imaging.Sprites;

namespace Arbiter.Imaging.Tests.Sprites;

public sealed class CreatureSpriteLoaderTests
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
    public void Should_Load_South_Facing_Idle_Frame_With_Embedded_Palette_Number()
    {
        TestImageData.WriteDat(
            _directory,
            "mock.dat",
            ("mns007.mpf", TestImageData.Mpf(
                2,
                1,
                [
                    new TestMpfFrame(0, 0, 2, 1, 1, 1, [1, 1]),
                    new TestMpfFrame(0, 0, 2, 1, 1, 1, [2, 3])
                ],
                paletteNumber: 3,
                walkFrameCount: 1,
                standingFrameIndex: 1,
                standingFrameCount: 1)),
            ("mns003.pal", TestImageData.Palette(
                (2, 10, 20, 30),
                (3, 40, 50, 60))));
        var loader = new CreatureSpriteLoader(DatAssetCatalog.Load(_directory));

        var preview = loader.LoadPreview(7);

        Assert.Multiple(() =>
        {
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview!.FrameCount, Is.EqualTo(1));
            Assert.That(preview.Width, Is.EqualTo(2));
            Assert.That(preview.Height, Is.EqualTo(1));
            Assert.That(preview.SourceImageName, Does.Contain("mns007.mpf"));
            Assert.That(preview.SourcePaletteName, Does.Contain("mns003.pal"));
            Assert.That(preview.Pixels.ToArray(), Is.EqualTo(new byte[]
            {
                40, 50, 60, 255,
                10, 20, 30, 255
            }));
        });
    }

    [Test]
    public void Should_Use_Second_Standing_Direction_Block_For_South_Facing_Preview()
    {
        TestImageData.WriteDat(
            _directory,
            "mock.dat",
            ("mns008.mpf", TestImageData.Mpf(
                1,
                1,
                [
                    new TestMpfFrame(0, 0, 1, 1, 0, 1, [1]),
                    new TestMpfFrame(0, 0, 1, 1, 0, 1, [2]),
                    new TestMpfFrame(0, 0, 1, 1, 0, 1, [3]),
                    new TestMpfFrame(0, 0, 1, 1, 0, 1, [4])
                ],
                standingFrameIndex: 2,
                standingFrameCount: 1,
                optionalAnimationFrameCount: 1)),
            ("mns000.pal", TestImageData.Palette(
                (3, 30, 0, 0),
                (4, 40, 0, 0))));
        var loader = new CreatureSpriteLoader(DatAssetCatalog.Load(_directory));

        var preview = loader.LoadPreview(8);

        Assert.That(preview!.Pixels.ToArray(), Is.EqualTo(new byte[] { 40, 0, 0, 255 }));
    }

    [Test]
    public void Should_Return_Null_When_Creature_Sprite_Is_Missing()
    {
        TestImageData.WriteDat(_directory, "mock.dat", ("mns000.pal", TestImageData.Palette()));
        var loader = new CreatureSpriteLoader(DatAssetCatalog.Load(_directory));

        Assert.That(loader.LoadPreview(99), Is.Null);
    }
}
