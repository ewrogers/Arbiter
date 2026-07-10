using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Dialogs;
using Arbiter.Net.Types;
using Avalonia.Media;

namespace Arbiter.App.Tests.ViewModels.Dialogs;

public sealed class DialogViewModelTests
{
    [Test]
    public void Should_Resolve_Creature_Dialog_Portraits()
    {
        var sprites = new RecordingSpriteService();
        var viewModel = new DialogViewModel(sprites)
        {
            Sprite = 42,
            SpriteType = SpriteType.Monster
        };

        _ = viewModel.SpriteImage;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasSprite, Is.True);
            Assert.That(viewModel.SpriteFallbackText, Is.EqualTo("#42"));
            Assert.That(sprites.LastCreatureSprite, Is.EqualTo(42));
            Assert.That(sprites.LastItemSprite, Is.Null);
        });
    }

    [Test]
    public void Should_Resolve_Full_Color_Item_Dialog_Portraits()
    {
        var sprites = new RecordingSpriteService();
        var viewModel = new DialogViewModel(sprites)
        {
            Sprite = 123,
            SpriteType = SpriteType.Item,
            Color = 7
        };

        _ = viewModel.SpriteImage;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SpriteFallbackText, Is.EqualTo("#123"));
            Assert.That(sprites.LastItemSprite, Is.EqualTo(123));
            Assert.That(sprites.LastItemColor, Is.EqualTo(7));
            Assert.That(sprites.LastCreatureSprite, Is.Null);
        });
    }

    [Test]
    public void Should_Show_None_When_Dialog_Has_No_Sprite()
    {
        var viewModel = new DialogViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasSprite, Is.False);
            Assert.That(viewModel.SpriteFallbackText, Is.EqualTo("None"));
            Assert.That(viewModel.SpriteImage, Is.Null);
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
