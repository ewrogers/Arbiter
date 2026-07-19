using System.Text.Json;
using Arbiter.App.Models.Settings;

namespace Arbiter.App.Tests.Models.Settings;

public sealed class ArbiterSettingsTests
{
    [Test]
    public void Should_Default_Legacy_Settings_To_Current_Options()
    {
        var settings = JsonSerializer.Deserialize<ArbiterSettings>("{}");

        Assert.Multiple(() =>
        {
            Assert.That(settings?.TraceDetailedView, Is.True);
            Assert.That(settings?.ApplyModifiersKeyFix, Is.True);
            Assert.That(settings?.AllowAltToShowGroundItems, Is.True);
            Assert.That(settings?.SkipQuantityPromptInExchange, Is.True);
            Assert.That(settings?.ShowItemQuantityInDialogs, Is.True);
            Assert.That(settings?.MakeExchangeDialogDraggable, Is.True);
            Assert.That(settings?.ShowExchangeResultsInMessageBar, Is.True);
        });
    }

    [Test]
    public void Should_Preserve_Trace_View_Preference_When_Cloned()
    {
        var settings = new ArbiterSettings { TraceDetailedView = false };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.That(clone.TraceDetailedView, Is.False);
    }

    [Test]
    public void Should_Preserve_Modifiers_Key_Fix_Preference_When_Cloned()
    {
        var settings = new ArbiterSettings { ApplyModifiersKeyFix = false };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.That(clone.ApplyModifiersKeyFix, Is.False);
    }

    [Test]
    public void Should_Preserve_Alt_Ground_Item_Preference_When_Cloned()
    {
        var settings = new ArbiterSettings { AllowAltToShowGroundItems = false };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.That(clone.AllowAltToShowGroundItems, Is.False);
    }

    [Test]
    public void Should_Preserve_Exchange_Quantity_Prompt_Preference_When_Cloned()
    {
        var settings = new ArbiterSettings { SkipQuantityPromptInExchange = false };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.That(clone.SkipQuantityPromptInExchange, Is.False);
    }

    [Test]
    public void Should_Preserve_Dialog_Item_Quantity_Preference_When_Cloned()
    {
        var settings = new ArbiterSettings { ShowItemQuantityInDialogs = false };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.That(clone.ShowItemQuantityInDialogs, Is.False);
    }

    [Test]
    public void Should_Preserve_Exchange_Ui_Preferences_When_Cloned()
    {
        var settings = new ArbiterSettings
        {
            MakeExchangeDialogDraggable = false,
            ShowExchangeResultsInMessageBar = false,
        };

        var clone = (ArbiterSettings)settings.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.MakeExchangeDialogDraggable, Is.False);
            Assert.That(clone.ShowExchangeResultsInMessageBar, Is.False);
        });
    }
}
