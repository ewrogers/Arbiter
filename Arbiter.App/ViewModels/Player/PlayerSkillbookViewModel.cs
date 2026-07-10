using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Arbiter.App.Collections;
using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arbiter.App.ViewModels.Player;

public partial class PlayerSkillbookViewModel : ViewModelBase
{
    private readonly ISlottedCollection<SkillbookItem> _skills;
    private readonly IGameSpriteService _spriteService;
    private readonly TimeProvider _timeProvider;

    [ObservableProperty] private PlayerSkillSlotViewModel? _selectedSkill;

    public ObservableCollection<PlayerSkillSlotViewModel> TemuairSkills { get; } = [];
    public ObservableCollection<PlayerSkillSlotViewModel> MedeniaSkills { get; } = [];
    public ObservableCollection<PlayerSkillSlotViewModel> WorldSkills { get; } = [];

    public PlayerSkillbookViewModel(ISlottedCollection<SkillbookItem> skills)
        : this(skills, NullGameSpriteService.Instance, TimeProvider.System)
    {
    }

    public PlayerSkillbookViewModel(
        ISlottedCollection<SkillbookItem> skills,
        IGameSpriteService spriteService,
        TimeProvider timeProvider)
    {
        _skills = skills;
        _spriteService = spriteService;
        _timeProvider = timeProvider;

        for (var i = 0; i < _skills.Capacity; i++)
        {
            if (i < 36)
            {
                TemuairSkills.Add(CreateSlot(i + 1));
            }
            else if (i < 72)
            {
                MedeniaSkills.Add(CreateSlot(i + 1));
            }
            else
            {
                WorldSkills.Add(CreateSlot(i + 1));
            }
        }

        _skills.ItemAdded += OnSkillAdded;
        _skills.ItemRemoved += OnSkillRemoved;
    }

    public int? GetFirstEmptySlot(int startSlot = 1) => _skills.GetFirstEmptySlot(startSlot);

    public bool HasSkill(string name) => GetSkill(name, out _);

    public bool GetSkill(string name, [NotNullWhen(true)] out Slotted<SkillbookItem>? skill)
    {
        skill = null;
        if (!_skills.TryGetValue(x => string.Equals(x.Value.Name, name, StringComparison.OrdinalIgnoreCase),
                out var found))
        {
            return false;
        }

        skill = found;
        return true;
    }
    
    public bool TryGetSlot(int slot, out Slotted<SkillbookItem> skill)
    {
        skill = default;
        if (!_skills.TryGetValue(x => x.Slot == slot, out var found))
        {
            return false;
        }
        
        skill = found;
        return true;
    }

    public void SetSlot(int slot, SkillbookItem item) => _skills.SetSlot(slot, item);
    public void ClearSlot(int slot) => _skills.ClearSlot(slot);

    public bool TryRemoveSkill(string name, out int slot)
    {
        slot = 0;
        if (!_skills.TryGetValue(x => string.Equals(x.Value.Name, name, StringComparison.OrdinalIgnoreCase),
                out var found))
        {
            return false;
        }

        slot = found.Slot;
        return true;
    }

    public bool UpdateCooldown(int slot, TimeSpan duration)
    {
        if (slot < 1 || slot > _skills.Capacity)
        {
            return false;
        }

        var index = slot - 1;
        var vm = index switch
        {
            < 36 => TemuairSkills[index],
            < 72 => MedeniaSkills[index - 36],
            _ => WorldSkills[index - 72]
        };
        
        return vm.SetCooldown(duration);
    }

    public bool TickCooldowns()
    {
        var hasCooldowns = false;
        foreach (var skill in TemuairSkills.Concat(MedeniaSkills).Concat(WorldSkills))
        {
            hasCooldowns |= skill.TickCooldown();
        }

        return hasCooldowns;
    }

    public void RefreshSprites()
    {
        foreach (var skill in TemuairSkills.Concat(MedeniaSkills).Concat(WorldSkills))
        {
            skill.RefreshSprite();
        }
    }

    private void OnSkillAdded(Slotted<SkillbookItem> skill)
    {
        if (skill.Slot < 1 || skill.Slot > _skills.Capacity)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnSkillAdded(skill));
            return;
        }

        SetSkillViewModel(skill.Slot, skill.Value);
    }

    private void OnSkillRemoved(Slotted<SkillbookItem> skill)
    {
        if (skill.Slot < 1 || skill.Slot > _skills.Capacity)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnSkillRemoved(skill));
            return;
        }

        SetSkillViewModel(skill.Slot);
    }

    private void SetSkillViewModel(int slot, SkillbookItem? skill = null)
    {
        if (slot < 1 || slot > _skills.Capacity)
        {
            return;
        }

        var index = (slot - 1) % 36;

        switch (slot - 1)
        {
            case < 36:
                TemuairSkills[index] = CreateSlot(slot, skill);
                break;
            case < 72:
                MedeniaSkills[index] = CreateSlot(slot, skill);
                break;
            default:
                WorldSkills[index] = CreateSlot(slot, skill);
                break;
        }
    }

    private PlayerSkillSlotViewModel CreateSlot(int slot, SkillbookItem? skill = null) =>
        new(slot, skill, _spriteService, _timeProvider);
}
