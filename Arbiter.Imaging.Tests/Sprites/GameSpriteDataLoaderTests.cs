using Arbiter.Imaging.Sprites;

namespace Arbiter.Imaging.Tests.Sprites;

public sealed class GameSpriteDataLoaderTests
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
    public void Should_Load_Available_Families_And_Report_Missing_Ones()
    {
        TestImageData.WriteDat(
            _directory,
            "mock.dat",
            ("skill001.epf", TestImageData.Epf(1, 1, [1])),
            ("gui06.pal", TestImageData.Palette((1, 100, 150, 200))));

        var data = GameSpriteDataLoader.Load(_directory);

        Assert.Multiple(() =>
        {
            Assert.That(data.Skills, Is.Not.Null);
            Assert.That(data.SkillsOnCooldown, Is.Not.Null);
            Assert.That(data.Spells, Is.Null);
            Assert.That(data.Items, Is.Null);
            Assert.That(data.Issues.Select(issue => issue.Category), Is.EquivalentTo(new[] { "spells", "items" }));
        });
    }
}
