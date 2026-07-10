using Arbiter.IO.Assets;
using Arbiter.IO.Tests.Archives;

namespace Arbiter.IO.Tests.Assets;

public sealed class DatAssetCatalogTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "Arbiter.IO.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, true);
    }

    [Test]
    public void Should_Prefer_Assets_From_Later_Archives()
    {
        MockDatArchive.Write(_directory, "a.dat", ("sprite.epf", [1]));
        MockDatArchive.Write(_directory, "b.dat", ("sprite.epf", [2]));

        var catalog = DatAssetCatalog.Load(_directory);
        var found = catalog.TryGet("SPRITE.EPF", out var asset);
        using var stream = asset!.OpenRead();

        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(asset.ArchiveName, Is.EqualTo("b.dat"));
            Assert.That(stream.ReadByte(), Is.EqualTo(2));
            Assert.That(catalog.GetAll("sprite.epf").Select(value => value.ArchiveName),
                Is.EqualTo(new[] { "a.dat", "b.dat" }));
        });
    }

    [Test]
    public void Should_Ignore_Non_Dat_Files()
    {
        MockDatArchive.Write(_directory, "assets.dat", ("value.bin", [1]));
        File.WriteAllBytes(Path.Combine(_directory, "ignored.bin"), [2]);

        var catalog = DatAssetCatalog.Load(_directory);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Archives, Has.Count.EqualTo(1));
            Assert.That(catalog.Names, Is.EquivalentTo(new[] { "value.bin" }));
        });
    }
}
