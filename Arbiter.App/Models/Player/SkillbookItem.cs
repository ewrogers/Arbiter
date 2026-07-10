using System;

namespace Arbiter.App.Models.Player;

public sealed class SkillbookItem
{
    public ushort Sprite { get; init; }
    public required string Name { get; init; }
    public int CurrentLevel { get; init; }
    public int MaxLevel { get; init; }
    public TimeSpan CooldownDuration { get; set; }
    public DateTimeOffset? CooldownUntil { get; set; }
    public bool IsVirtual { get; init; }
    public Action? OnUse { get; init; }

    public override string ToString()
        => MaxLevel > 0 ? $"{Name} (Level: {CurrentLevel}/{MaxLevel})" : Name;
}
