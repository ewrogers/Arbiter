using Arbiter.App.Models.Player;
using Arbiter.App.Services.Sprites;
using Arbiter.App.ViewModels.Player;
using Avalonia.Media;

namespace Arbiter.App.Tests.ViewModels.Player;

public sealed class PlayerSkillSlotViewModelTests
{
    [Test]
    public void Should_Replace_Cooldown_With_Every_Authoritative_Server_Update()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero));
        var skill = new SkillbookItem { Name = "Assail", Sprite = 7 };
        var viewModel = new PlayerSkillSlotViewModel(1, skill, new RecordingSpriteService(), timeProvider);

        viewModel.SetCooldown(TimeSpan.FromMinutes(2));
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        viewModel.SetCooldown(TimeSpan.FromSeconds(30));

        Assert.Multiple(() =>
        {
            Assert.That(skill.CooldownDuration, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(skill.CooldownUntil, Is.EqualTo(timeProvider.GetUtcNow().AddSeconds(30)));
            Assert.That(viewModel.CooldownText, Is.EqualTo("30s"));
            Assert.That(viewModel.CooldownDurationText, Is.EqualTo("30s"));
        });

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        viewModel.SetCooldown(TimeSpan.FromSeconds(90));

        Assert.Multiple(() =>
        {
            Assert.That(skill.CooldownDuration, Is.EqualTo(TimeSpan.FromSeconds(90)));
            Assert.That(skill.CooldownUntil, Is.EqualTo(timeProvider.GetUtcNow().AddSeconds(90)));
            Assert.That(viewModel.CooldownText, Is.EqualTo("2m"));
            Assert.That(viewModel.CooldownDurationText, Is.EqualTo("2m"));
        });

        timeProvider.Advance(TimeSpan.FromSeconds(90));
        viewModel.TickCooldown();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasCooldown, Is.False);
            Assert.That(viewModel.HasCooldownDuration, Is.True);
            Assert.That(viewModel.CooldownDurationText, Is.EqualTo("2m"));
        });
    }

    [Test]
    public void Should_Tick_Cooldown_To_Expiration_And_Select_Tinted_Sprite_State()
    {
        var timeProvider = new ManualTimeProvider(new DateTimeOffset(2026, 7, 9, 12, 0, 0, TimeSpan.Zero));
        var sprites = new RecordingSpriteService();
        var skill = new SkillbookItem { Name = "Assail", Sprite = 7 };
        var viewModel = new PlayerSkillSlotViewModel(1, skill, sprites, timeProvider);

        viewModel.SetCooldown(TimeSpan.FromMinutes(2));
        _ = viewModel.SpriteImage;
        timeProvider.Advance(TimeSpan.FromSeconds(61));
        viewModel.TickCooldown();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasCooldown, Is.True);
            Assert.That(viewModel.CooldownText, Is.EqualTo("59s"));
            Assert.That(sprites.LastSkillCooldownState, Is.True);
        });

        timeProvider.Advance(TimeSpan.FromSeconds(59));
        viewModel.TickCooldown();
        _ = viewModel.SpriteImage;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasCooldown, Is.False);
            Assert.That(viewModel.CooldownText, Is.Empty);
            Assert.That(skill.CooldownUntil, Is.Null);
            Assert.That(sprites.LastSkillCooldownState, Is.False);
        });
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }

    private sealed class RecordingSpriteService : IGameSpriteService
    {
        public bool LastSkillCooldownState { get; private set; }

        public event EventHandler? SpritesChanged
        {
            add { }
            remove { }
        }

        public Task LoadAsync(string clientExecutablePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public IImage? GetItem(ushort sprite, byte color) => null;

        public IImage? GetSkill(ushort sprite, bool isOnCooldown)
        {
            LastSkillCooldownState = isOnCooldown;
            return null;
        }

        public IImage? GetSpell(ushort sprite, bool isOnCooldown) => null;
    }
}
