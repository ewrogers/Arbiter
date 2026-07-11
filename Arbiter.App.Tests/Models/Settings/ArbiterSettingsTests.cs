using System.Text.Json;
using Arbiter.App.Models.Settings;

namespace Arbiter.App.Tests.Models.Settings;

public sealed class ArbiterSettingsTests
{
    [Test]
    public void Should_Default_Legacy_Settings_To_Detailed_Trace_View()
    {
        var settings = JsonSerializer.Deserialize<ArbiterSettings>("{}");

        Assert.That(settings?.TraceDetailedView, Is.True);
    }

    [Test]
    public void Should_Preserve_Trace_View_Preference_When_Cloned()
    {
        var settings = new ArbiterSettings { TraceDetailedView = false };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.That(clone.TraceDetailedView, Is.False);
    }
}
