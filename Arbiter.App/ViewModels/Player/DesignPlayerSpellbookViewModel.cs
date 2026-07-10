using System;
using Arbiter.App.Collections;
using Arbiter.App.Models.Player;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Player;

public sealed class DesignPlayerSpellbookViewModel : PlayerSpellbookViewModel
{
    public DesignPlayerSpellbookViewModel()
        : base(CreateTestSpellbook())
    {
    }

    private static ISlottedCollection<SpellbookItem> CreateTestSpellbook()
    {
        var spellbook = new SlottedCollection<SpellbookItem>(PlayerState.MaxTemuairSpells +
                                                             PlayerState.MaxMedeniaSpells + PlayerState.MaxWorldSpells);
        spellbook.SetSlot(1, new SpellbookItem
        {
            Name = "Fas Spiorad",
            Sprite = 1,
            TargetType = SpellTargetType.Target,
            CurrentLevel = 50,
            MaxLevel = 100,
            CooldownDuration = TimeSpan.FromSeconds(59),
            CooldownUntil = DateTimeOffset.Now.AddSeconds(59)
        });

        return spellbook;
    }
}
