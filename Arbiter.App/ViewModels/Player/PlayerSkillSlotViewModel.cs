using System;
using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Avalonia.Media;

namespace Arbiter.App.ViewModels.Player;

public sealed class PlayerSkillSlotViewModel : ViewModelBase
{
    private readonly SkillbookItem? _skill;
    private readonly IGameSpriteService _spriteService;
    private readonly TimeProvider _timeProvider;
    private string _cooldownText = string.Empty;
    private bool _hasCooldown;

    public int Slot { get; }

    public int RelativeSlot
    {
        get
        {
            return Slot switch
            {
                <= 36 => Slot,
                <= 72 => Slot - 36,
                _ => Slot - 72
            };
        }
    }

    public bool IsEmpty => _skill is null;
    public string Name => _skill?.Name ?? string.Empty;
    public ushort Sprite => _skill?.Sprite ?? 0;
    public int CurrentLevel => _skill?.CurrentLevel ?? 0;
    public int MaxLevel => _skill?.MaxLevel ?? 0;
    public TimeSpan Cooldown => _skill?.CooldownUntil is { } until
        ? TimeSpan.FromTicks(Math.Max(0, (until - _timeProvider.GetUtcNow()).Ticks))
        : TimeSpan.Zero;
    public bool HasLevel => _skill?.MaxLevel > 0;
    public bool HasCooldown => _hasCooldown;
    public bool HasCooldownDuration => _skill?.CooldownDuration > TimeSpan.Zero;
    public string CooldownText => _cooldownText;
    public string CooldownDurationText => CooldownFormatter.Format(_skill?.CooldownDuration ?? TimeSpan.Zero);
    public bool IsVirtual => _skill?.IsVirtual ?? false;
    public IImage? SpriteImage => _skill is null ? null : _spriteService.GetSkill(Sprite, HasCooldown);
    public string SpriteFallbackText => IsEmpty ? string.Empty : $"#{Sprite}";

    public PlayerSkillSlotViewModel(int slot, SkillbookItem? skill = null)
        : this(slot, skill, NullGameSpriteService.Instance, TimeProvider.System)
    {
    }

    public PlayerSkillSlotViewModel(
        int slot,
        SkillbookItem? skill,
        IGameSpriteService spriteService,
        TimeProvider timeProvider)
    {
        Slot = slot;
        _skill = skill;
        _spriteService = spriteService;
        _timeProvider = timeProvider;
        UpdateCooldown(_timeProvider.GetUtcNow());
    }

    public bool SetCooldown(TimeSpan duration)
    {
        if (_skill is null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var previousDuration = _skill.CooldownDuration;
        _skill.CooldownDuration = duration;
        _skill.CooldownUntil = duration > TimeSpan.Zero ? now + duration : null;
        if (previousDuration != duration)
        {
            OnPropertyChanged(nameof(HasCooldownDuration));
            OnPropertyChanged(nameof(CooldownDurationText));
        }

        return UpdateCooldown(now);
    }

    public bool TickCooldown() => UpdateCooldown(_timeProvider.GetUtcNow());
    public void RefreshSprite() => OnPropertyChanged(nameof(SpriteImage));

    public override string ToString() => IsEmpty ? "<empty>" : Name;

    private bool UpdateCooldown(DateTimeOffset now)
    {
        var cooldownText = CooldownFormatter.Format(_skill?.CooldownUntil, now);
        var hasCooldown = !string.IsNullOrEmpty(cooldownText);
        if (!hasCooldown && _skill is not null)
        {
            _skill.CooldownUntil = null;
        }

        if (!string.Equals(_cooldownText, cooldownText, StringComparison.Ordinal))
        {
            _cooldownText = cooldownText;
            OnPropertyChanged(nameof(CooldownText));
            OnPropertyChanged(nameof(Cooldown));
        }

        if (_hasCooldown != hasCooldown)
        {
            _hasCooldown = hasCooldown;
            OnPropertyChanged(nameof(HasCooldown));
            OnPropertyChanged(nameof(SpriteImage));
        }

        return hasCooldown;
    }
}
