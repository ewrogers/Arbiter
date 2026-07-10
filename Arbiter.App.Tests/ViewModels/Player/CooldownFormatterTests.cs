using Arbiter.App.ViewModels.Player;

namespace Arbiter.App.Tests.ViewModels.Player;

public sealed class CooldownFormatterTests
{
    [TestCase(120, "2m")]
    [TestCase(119, "2m")]
    [TestCase(60, "1m")]
    [TestCase(59, "59s")]
    [TestCase(1, "1s")]
    [TestCase(0, "")]
    [TestCase(-1, "")]
    public void Should_Format_Wow_Style_Minutes_And_Seconds(int seconds, string expected)
    {
        Assert.That(CooldownFormatter.Format(TimeSpan.FromSeconds(seconds)), Is.EqualTo(expected));
    }
}
