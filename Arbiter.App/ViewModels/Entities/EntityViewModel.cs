using Arbiter.App.Models.Entities;
using Arbiter.App.Services.Sprites;
using Arbiter.Net.Types;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arbiter.App.ViewModels.Entities;

public partial class EntityViewModel : ViewModelBase
{
    private readonly IGameSpriteService _spriteService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Flags), nameof(Id), nameof(Name), nameof(TypeName), nameof(TypeShorthand),
        nameof(NameOrTypeName), nameof(Sprite), nameof(SpriteColor), nameof(SpriteImage), nameof(SpriteFallbackText),
        nameof(MapId), nameof(MapName), nameof(X), nameof(Y), nameof(Position), nameof(IsHidden), nameof(IsGhost))]
    [NotifyPropertyChangedFor(nameof(IsPlayer), nameof(IsMonster), nameof(IsMundane), nameof(IsItem),
        nameof(IsReactor), nameof(IsCreature), nameof(ShowsSpritePreview))]
    private GameEntity _entity;

    [ObservableProperty] private double _opacity = 1;

    public long SortIndex { get; init; }

    public EntityFlags Flags => Entity.Flags;
    public long Id => Entity.Id;

    public string? Name => Entity.Name;
    public string NameOrTypeName => !string.IsNullOrWhiteSpace(Entity.Name) ? Entity.Name : TypeName;

    public string TypeName => Flags switch
    {
        EntityFlags.Player => "Player",
        EntityFlags.Monster => "Monster",
        EntityFlags.Mundane => "NPC",
        EntityFlags.Item => "Item",
        EntityFlags.Reactor => "Reactor",
        _ => "Unknown"
    };

    public string TypeShorthand => Flags switch
    {
        EntityFlags.Player => "U",
        EntityFlags.Monster => "M",
        EntityFlags.Mundane => "N",
        EntityFlags.Item => "I",
        EntityFlags.Reactor => "R",
        _ => "?"
    };

    public ushort Sprite => Entity.Sprite ?? 0;
    public byte SpriteColor => Entity.Color ?? 0;
    public IImage? SpriteImage => IsItem
        ? _spriteService.GetItem(Sprite, SpriteColor)
        : IsCreature
            ? _spriteService.GetCreature(Sprite)
            : null;
    public string SpriteFallbackText => ShowsSpritePreview ? $"#{Sprite}" : string.Empty;
    public int MapId => Entity.MapId ?? 0;
    public string MapName => Entity.MapName ?? "Unknown Map";
    public int X => Entity.X;
    public int Y => Entity.Y;
    public string Position => $"{X}, {Y}";
    public bool IsHidden => Flags.HasFlag(EntityFlags.Player) && Sprite == 0;

    public bool IsGhost => Flags.HasFlag(EntityFlags.Player) &&
                           ((Sprite & (ushort)BodySprite.MaleGhost) == (ushort)BodySprite.MaleGhost ||
                            (Sprite & (ushort)BodySprite.FemaleGhost) == (ushort)BodySprite.FemaleGhost);

    public bool IsPlayer => Flags.HasFlag(EntityFlags.Player);
    public bool IsMonster => Flags.HasFlag(EntityFlags.Monster);
    public bool IsMundane => Flags.HasFlag(EntityFlags.Mundane);
    public bool IsItem => Flags.HasFlag(EntityFlags.Item);
    public bool IsReactor => Flags.HasFlag(EntityFlags.Reactor);
    public bool IsCreature => IsMonster || IsMundane;
    public bool ShowsSpritePreview => IsCreature || IsItem;

    public EntityViewModel(GameEntity entity)
        : this(entity, NullGameSpriteService.Instance)
    {
    }

    public EntityViewModel(GameEntity entity, IGameSpriteService spriteService)
    {
        Entity = entity;
        _spriteService = spriteService;
    }

    public void RefreshSprite() => OnPropertyChanged(nameof(SpriteImage));
}
