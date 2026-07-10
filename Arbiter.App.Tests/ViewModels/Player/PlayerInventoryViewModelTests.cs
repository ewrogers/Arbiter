using Arbiter.App.Collections;
using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Player;
using Avalonia.Media;

namespace Arbiter.App.Tests.ViewModels.Player;

public sealed class PlayerInventoryViewModelTests
{
    [Test]
    public void Should_Reserve_Last_Inventory_Slot_For_Gold()
    {
        var inventory = new SlottedCollection<InventoryItem>(PlayerState.MaxInventorySlots);
        var viewModel = new PlayerInventoryViewModel(inventory, new NullSprites());

        var gold = viewModel.InventorySlots[^1];

        Assert.Multiple(() =>
        {
            Assert.That(gold.Slot, Is.EqualTo(60));
            Assert.That(gold.IsGold, Is.True);
            Assert.That(gold.IsEmpty, Is.False);
            Assert.That(gold.Name, Is.EqualTo("Gold"));
            Assert.That(gold.Sprite, Is.EqualTo(PlayerInventorySlotViewModel.GoldSprite));
            Assert.That(gold.Quantity, Is.Zero);
            Assert.That(gold.ShowsQuantity, Is.False);
        });
    }

    [Test]
    public void Should_Show_Compact_And_Exact_Gold_Counts()
    {
        var gold = PlayerInventorySlotViewModel.CreateGold(60, 1_234_567, new NullSprites());

        Assert.Multiple(() =>
        {
            Assert.That(gold.DetailText, Is.EqualTo("1.2m"));
            Assert.That(gold.GoldAmountText, Is.EqualTo($"{1_234_567:N0} gold"));
            Assert.That(gold.SpriteFallbackText, Is.EqualTo("#136"));
        });
    }

    [Test]
    public void Should_Show_Item_Quantities_At_The_Bottom_Of_The_Slot()
    {
        var item = new InventoryItem
        {
            Name = "Cherry",
            Sprite = 1,
            Quantity = 4,
            IsStackable = true
        };
        var viewModel = new PlayerInventorySlotViewModel(1, item, new NullSprites());

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ShowsQuantity, Is.True);
            Assert.That(viewModel.QuantityDisplayText, Is.EqualTo("(x4)"));
            Assert.That(viewModel.DetailText, Is.EqualTo("(x4)"));
        });
    }

    [Test]
    public void Should_Hide_Item_Quantity_When_Only_One_Remains()
    {
        var item = new InventoryItem
        {
            Name = "Cherry",
            Sprite = 1,
            Quantity = 1,
            IsStackable = true
        };
        var viewModel = new PlayerInventorySlotViewModel(1, item, new NullSprites());

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ShowsQuantity, Is.False);
            Assert.That(viewModel.DetailText, Is.Empty);
        });
    }

    private sealed class NullSprites : IGameSpriteService
    {
        public event EventHandler? SpritesChanged
        {
            add { }
            remove { }
        }

        public Task LoadAsync(string clientExecutablePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public IImage? GetCreature(ushort sprite) => null;
        public IImage? GetItem(ushort sprite, byte color) => null;
        public IImage? GetSkill(ushort sprite, bool isOnCooldown) => null;
        public IImage? GetSpell(ushort sprite, bool isOnCooldown) => null;
    }
}
