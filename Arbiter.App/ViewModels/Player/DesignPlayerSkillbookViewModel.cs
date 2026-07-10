using System;
using Arbiter.App.Collections;
using Arbiter.App.Models.Player;

namespace Arbiter.App.ViewModels.Player;

public sealed class DesignPlayerSkillbookViewModel : PlayerSkillbookViewModel
{
    public DesignPlayerSkillbookViewModel()
        : base(CreateTestSkillbook())
    {
    }

    public static ISlottedCollection<SkillbookItem> CreateTestSkillbook()
    {
        var skills = new SlottedCollection<SkillbookItem>(PlayerState.MaxTemuairSkills + PlayerState.MaxMedeniaSkills +
                                                          PlayerState.MaxWorldSkills);
        var skill = new SkillbookItem
        {
            Name = "Assail",
            Sprite = 1,
            CurrentLevel = 80,
            MaxLevel = 100,
            CooldownDuration = TimeSpan.FromMinutes(2),
            CooldownUntil = DateTimeOffset.Now.AddMinutes(2)
        };

        skills.SetSlot(1, skill);
        return skills;
    }
}
