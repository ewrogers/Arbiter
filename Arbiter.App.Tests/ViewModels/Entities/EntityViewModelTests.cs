using Arbiter.App.Models.Entities;
using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Entities;
using Avalonia.Media;

namespace Arbiter.App.Tests.ViewModels.Entities;

public sealed class EntityViewModelTests
{
    [TestCase(EntityFlags.Monster)]
    [TestCase(EntityFlags.Mundane)]
    public void Should_Resolve_Creature_Sprites_For_Monster_And_Npc_Tooltips(EntityFlags flags)
    {
        var sprites = new RecordingSpriteService();
        var viewModel = new EntityViewModel(
            new GameEntity { Id = 1, Flags = flags, Sprite = 42 },
            sprites);

        _ = viewModel.SpriteImage;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ShowsSpritePreview, Is.True);
            Assert.That(viewModel.SpriteFallbackText, Is.EqualTo("#42"));
            Assert.That(sprites.LastCreatureSprite, Is.EqualTo(42));
            Assert.That(sprites.LastItemSprite, Is.Null);
        });
    }

    [Test]
    public void Should_Resolve_Full_Color_Item_Sprites_For_Ground_Item_Tooltips()
    {
        var sprites = new RecordingSpriteService();
        var viewModel = new EntityViewModel(
            new GameEntity { Id = 2, Flags = EntityFlags.Item, Sprite = 123, Color = 7 },
            sprites);

        _ = viewModel.SpriteImage;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ShowsSpritePreview, Is.True);
            Assert.That(viewModel.SpriteFallbackText, Is.EqualTo("#123"));
            Assert.That(sprites.LastItemSprite, Is.EqualTo(123));
            Assert.That(sprites.LastItemColor, Is.EqualTo(7));
            Assert.That(sprites.LastCreatureSprite, Is.Null);
        });
    }

    private sealed class RecordingSpriteService : IGameSpriteService
    {
        public ushort? LastCreatureSprite { get; private set; }
        public ushort? LastItemSprite { get; private set; }
        public byte? LastItemColor { get; private set; }

        public event EventHandler? SpritesChanged
        {
            add { }
            remove { }
        }

        public Task LoadAsync(string clientExecutablePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public IImage? GetCreature(ushort sprite)
        {
            LastCreatureSprite = sprite;
            return null;
        }

        public IImage? GetItem(ushort sprite, byte color)
        {
            LastItemSprite = sprite;
            LastItemColor = color;
            return null;
        }

        public IImage? GetSkill(ushort sprite, bool isOnCooldown) => null;
        public IImage? GetSpell(ushort sprite, bool isOnCooldown) => null;
    }
}
