using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Avalonia.Media;

namespace Arbiter.App.ViewModels.Player;

public class PlayerInventorySlotViewModel : ViewModelBase
{
    public const ushort GoldSprite = 136;

    private readonly InventoryItem? _item;
    private readonly IGameSpriteService _spriteService;
    private readonly bool _isGold;

    public int Slot { get; }
    public bool IsEmpty => _item is null;
    public string Name => _item?.Name ?? string.Empty;
    public ushort Sprite => _item?.Sprite ?? 0;
    public byte Color => _item?.Color ?? 0;
    public bool IsStackable => _item?.IsStackable ?? false;
    public bool IsGold => _isGold;
    public bool ShowsQuantity => !IsGold && Quantity > 1;
    public long Quantity => _item?.Quantity ?? 0;
    public string QuantityDisplayText => $"(x{Quantity:N0})";
    public string GoldAmountText => $"{Quantity:N0} gold";
    public string DetailText => IsGold
        ? FormatCompactQuantity(Quantity)
        : ShowsQuantity
            ? QuantityDisplayText
            : string.Empty;
    public long Durability => _item?.Durability ?? 0;
    public long MaxDurability => _item?.MaxDurability ?? 0;
    public bool HasDurability => Durability > 0 && MaxDurability > 0;
    public int PercentDurability => HasDurability ? (int)(Durability * 100 / MaxDurability) : 100;
    public bool IsVirtual => _item?.IsVirtual ?? false;
    public IImage? SpriteImage => _item is null ? null : _spriteService.GetItem(Sprite, Color);
    public string SpriteFallbackText => IsEmpty ? string.Empty : $"#{Sprite}";

    public PlayerInventorySlotViewModel(int slot, InventoryItem? item = null)
        : this(slot, item, NullGameSpriteService.Instance)
    {
    }

    public PlayerInventorySlotViewModel(int slot, InventoryItem? item, IGameSpriteService spriteService)
        : this(slot, item, spriteService, false)
    {
    }

    private PlayerInventorySlotViewModel(
        int slot,
        InventoryItem? item,
        IGameSpriteService spriteService,
        bool isGold)
    {
        Slot = slot;
        _item = item;
        _spriteService = spriteService;
        _isGold = isGold;
    }

    public static PlayerInventorySlotViewModel CreateGold(
        int slot,
        uint gold,
        IGameSpriteService spriteService)
    {
        return new PlayerInventorySlotViewModel(
            slot,
            new InventoryItem
            {
                Name = "Gold",
                Sprite = GoldSprite,
                Quantity = gold
            },
            spriteService,
            true);
    }

    public void RefreshSprite() => OnPropertyChanged(nameof(SpriteImage));

    public override string ToString()
    {
        if (IsEmpty)
        {
            return "<empty>";
        }

        return ShowsQuantity ? $"{Name} [{Quantity}]" : Name;
    }

    private static string FormatCompactQuantity(long quantity)
    {
        return quantity switch
        {
            < 1_000 => quantity.ToString(),
            < 1_000_000 => $"{quantity / 1_000.0:0.#}k",
            < 1_000_000_000 => $"{quantity / 1_000_000.0:0.#}m",
            _ => $"{quantity / 1_000_000_000.0:0.#}b"
        };
    }
}
