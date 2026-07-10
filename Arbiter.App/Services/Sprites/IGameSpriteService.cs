using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;

namespace Arbiter.App.Services.Sprites;

public interface IGameSpriteService
{
    event EventHandler? SpritesChanged;

    Task LoadAsync(string clientExecutablePath, CancellationToken cancellationToken = default);
    IImage? GetCreature(ushort sprite);
    IImage? GetItem(ushort sprite, byte color);
    IImage? GetSkill(ushort sprite, bool isOnCooldown);
    IImage? GetSpell(ushort sprite, bool isOnCooldown);
}
