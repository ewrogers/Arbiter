using System;
using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Arbiter.Net.Types;
using Avalonia.Media;

namespace Arbiter.App.ViewModels.Player;

public sealed class PlayerSpellSlotViewModel : ViewModelBase
{
    private readonly SpellbookItem? _spell;
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

    public bool IsEmpty => _spell is null;
    public string Name => _spell?.Name ?? string.Empty;
    public SpellTargetType TargetType => _spell?.TargetType ?? SpellTargetType.None;
    public ushort Sprite => _spell?.Sprite ?? 0;
    public int CurrentLevel => _spell?.CurrentLevel ?? 0;
    public int MaxLevel => _spell?.MaxLevel ?? 0;
    public TimeSpan Cooldown => _spell?.CooldownUntil is { } until
        ? TimeSpan.FromTicks(Math.Max(0, (until - _timeProvider.GetUtcNow()).Ticks))
        : TimeSpan.Zero;
    public bool HasLevel => _spell?.MaxLevel > 0;
    public bool HasCooldown => _hasCooldown;
    public bool HasCooldownDuration => _spell?.CooldownDuration > TimeSpan.Zero;
    public string CooldownText => _cooldownText;
    public string CooldownDurationText => CooldownFormatter.Format(_spell?.CooldownDuration ?? TimeSpan.Zero);
    public int CastLines => _spell?.CastLines ?? 0;
    public string CastLinesText => CastLines switch
    {
        0 => "Instant",
        1 => "1 line",
        _ => $"{CastLines} lines"
    };
    public bool IsVirtual => _spell?.IsVirtual ?? false;
    public IImage? SpriteImage => _spell is null ? null : _spriteService.GetSpell(Sprite, HasCooldown);
    public string SpriteFallbackText => IsEmpty ? string.Empty : $"#{Sprite}";

    public string TargetTypeText => TargetType switch
    {
        SpellTargetType.Target => "Target",
        SpellTargetType.Prompt => "Text Prompt",
        SpellTargetType.PromptOneNumber => "Numeric Prompt",
        SpellTargetType.PromptTwoNumbers => "Numeric Prompt (2)",
        SpellTargetType.PromptThreeNumbers => "Numeric Prompt (3)",
        SpellTargetType.PromptFourNumbers => "Numeric Prompt (4)",
        _ => "No Target"
    };

    public PlayerSpellSlotViewModel(int slot, SpellbookItem? spell = null)
        : this(slot, spell, NullGameSpriteService.Instance, TimeProvider.System)
    {
    }

    public PlayerSpellSlotViewModel(
        int slot,
        SpellbookItem? spell,
        IGameSpriteService spriteService,
        TimeProvider timeProvider)
    {
        Slot = slot;
        _spell = spell;
        _spriteService = spriteService;
        _timeProvider = timeProvider;
        UpdateCooldown(_timeProvider.GetUtcNow());
    }

    public bool SetCooldown(TimeSpan duration)
    {
        if (_spell is null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var previousDuration = _spell.CooldownDuration;
        _spell.CooldownDuration = duration;
        _spell.CooldownUntil = duration > TimeSpan.Zero ? now + duration : null;
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
        var cooldownText = CooldownFormatter.Format(_spell?.CooldownUntil, now);
        var hasCooldown = !string.IsNullOrEmpty(cooldownText);
        if (!hasCooldown && _spell is not null)
        {
            _spell.CooldownUntil = null;
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
