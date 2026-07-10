using Arbiter.IO.Assets;
using Arbiter.Imaging.Sprites;

namespace Arbiter.Imaging.Tests.Sprites;

public sealed class ItemSpriteAtlasLoaderTests
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
    public void Should_Map_Item_Ranges_And_Prefer_Legend_Archive_Variants()
    {
        var palette = TestImageData.Palette((1, 200, 10, 20), (2, 10, 200, 20));
        TestImageData.WriteDat(
            _directory,
            "Legend.dat",
            ("item001.epf", TestImageData.Epf(1, 1, [1])),
            ("item000.pal", palette),
            ("itempal.tbl", TestImageData.Text("1 1 0\n")));
        TestImageData.WriteDat(
            _directory,
            "setoa.dat",
            ("item001.epf", TestImageData.Epf(1, 1, [2])));

        var items = ItemSpriteAtlasLoader.Load(DatAssetCatalog.Load(_directory));
        var atlas = items.Atlases.Single();

        Assert.Multiple(() =>
        {
            Assert.That(atlas.FirstItemId, Is.EqualTo(1));
            Assert.That(atlas.LastItemId, Is.EqualTo(1));
            Assert.That(atlas.BaseAtlas.SourceImageName, Does.Contain("Legend.dat"));
            Assert.That(atlas.BaseAtlas.Pixels.ToArray(), Is.EqualTo(new byte[] { 200, 10, 20, 255 }));
            Assert.That(items.Find(1), Is.SameAs(atlas));
            Assert.That(items.Find(267), Is.Null);
        });
    }

    [Test]
    public void Should_Build_Dyed_Item_Variants_From_Mock_Tables()
    {
        var palette = TestImageData.Palette(
            (98, 10, 10, 10),
            (99, 20, 20, 20),
            (100, 30, 30, 30),
            (101, 40, 40, 40),
            (102, 50, 50, 50),
            (103, 60, 60, 60));
        const string dyeTable = """
                                6
                                0
                                10,10,10
                                20,20,20
                                30,30,30
                                40,40,40
                                50,50,50
                                60,60,60
                                1
                                200,100,50
                                201,101,51
                                202,102,52
                                203,103,53
                                204,104,54
                                205,105,55
                                """;
        TestImageData.WriteDat(
            _directory,
            "mock.dat",
            ("item001.epf", TestImageData.Epf(1, 1, [98])),
            ("item000.pal", palette),
            ("itempal.tbl", TestImageData.Text("1 1 0\n")),
            ("color0.tbl", TestImageData.Text(dyeTable)));

        var atlas = ItemSpriteAtlasLoader.Load(DatAssetCatalog.Load(_directory)).Atlases.Single();
        var variant = atlas.BuildColorVariant(1);

        Assert.Multiple(() =>
        {
            Assert.That(atlas.BaseAtlas.Pixels.ToArray(), Is.EqualTo(new byte[] { 10, 10, 10, 255 }));
            Assert.That(variant.Pixels.ToArray(), Is.EqualTo(new byte[] { 200, 100, 50, 255 }));
        });
    }
}
