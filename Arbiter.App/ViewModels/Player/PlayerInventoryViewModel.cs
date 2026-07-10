using System;
using System.Collections.ObjectModel;
using Arbiter.App.Collections;
using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arbiter.App.ViewModels.Player;

public partial class PlayerInventoryViewModel : ViewModelBase
{
    public const int GoldSlot = PlayerState.MaxInventorySlots;

    private readonly ISlottedCollection<InventoryItem> _inventory;
    private readonly IGameSpriteService _spriteService;

    [ObservableProperty] private PlayerInventorySlotViewModel? _selectedItem;

    public ObservableCollection<PlayerInventorySlotViewModel> InventorySlots { get; } = [];

    public PlayerInventoryViewModel(ISlottedCollection<InventoryItem> inventory)
        : this(inventory, NullGameSpriteService.Instance)
    {
    }

    public PlayerInventoryViewModel(
        ISlottedCollection<InventoryItem> inventory,
        IGameSpriteService spriteService)
    {
        _inventory = inventory;
        _spriteService = spriteService;

        for (var i = 0; i < inventory.Capacity; i++)
        {
            var slot = i + 1;
            InventorySlots.Add(slot == GoldSlot ? CreateGoldSlot(0) : CreateSlot(slot));
        }

        _inventory.ItemAdded += OnItemAdded;
        _inventory.ItemRemoved += OnItemRemoved;
    }
    
    public int? GetFirstEmptySlot()
    {
        foreach (var slot in _inventory.GetEmptySlots())
        {
            if (slot < GoldSlot)
            {
                return slot;
            }
        }

        return null;
    }

    public bool HasItem(string name) => GetItem(name, out _);

    public bool GetItem(string name, out Slotted<InventoryItem> item)
    {
        item = default;
        if (!_inventory.TryGetValue(x => string.Equals(x.Value.Name, name, StringComparison.OrdinalIgnoreCase),
                out var found))
        {
            return false;
        }

        item = default;
        return true;
    }

    public bool TryGetSlot(int slot, out Slotted<InventoryItem> item)
    {
        item = default;
        if (!_inventory.TryGetValue(x => x.Slot == slot, out var found))
        {
            return false;
        }

        item = default;
        return true;
    }

    public void SetSlot(int slot, InventoryItem item)
        => _inventory.SetSlot(slot, item);

    public void ClearSlot(int slot)
        => _inventory.ClearSlot(slot);

    public void SetGold(uint gold)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => SetGold(gold));
            return;
        }

        InventorySlots[GoldSlot - 1] = CreateGoldSlot(gold);
    }

    public void RefreshSprites()
    {
        foreach (var slot in InventorySlots)
        {
            slot.RefreshSprite();
        }
    }

    private void OnItemAdded(Slotted<InventoryItem> item)
    {
        if (item.Slot < 1 || item.Slot >= GoldSlot)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnItemAdded(item));
            return;
        }

        InventorySlots[item.Slot - 1] = CreateSlot(item.Slot, item.Value);
    }

    private void OnItemRemoved(Slotted<InventoryItem> item)
    {
        if (item.Slot < 1 || item.Slot >= GoldSlot)
        {
            return;
        }

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => OnItemRemoved(item));
            return;
        }

        InventorySlots[item.Slot - 1] = CreateSlot(item.Slot);
    }

    private PlayerInventorySlotViewModel CreateSlot(int slot, InventoryItem? item = null) =>
        new(slot, item, _spriteService);

    private PlayerInventorySlotViewModel CreateGoldSlot(uint gold) =>
        PlayerInventorySlotViewModel.CreateGold(GoldSlot, gold, _spriteService);
}
