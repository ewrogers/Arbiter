using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace Arbiter.App.Services.Sprites;

internal sealed class NullGameSpriteService : IGameSpriteService
{
    public static NullGameSpriteService Instance { get; } = new();

    private NullGameSpriteService()
    {
    }

    public event EventHandler? SpritesChanged
    {
        add { }
        remove { }
    }

    public Task LoadAsync(string clientExecutablePath, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public IImage? GetItem(ushort sprite, byte color) => null;
    public IImage? GetSkill(ushort sprite, bool isOnCooldown) => null;
    public IImage? GetSpell(ushort sprite, bool isOnCooldown) => null;
}
