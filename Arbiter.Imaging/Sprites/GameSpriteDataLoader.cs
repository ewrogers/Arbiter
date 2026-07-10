using Arbiter.IO.Assets;

namespace Arbiter.Imaging.Sprites;

public static class GameSpriteDataLoader
{
    public const byte CooldownTintRed = 147;
    public const byte CooldownTintGreen = 189;
    public const byte CooldownTintBlue = 255;

    public static GameSpriteData Load(string assetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetDirectory);
        var catalog = DatAssetCatalog.Load(assetDirectory);
        var issues = new List<GameSpriteLoadIssue>();

        var (skills, skillsOnCooldown) = LoadAbility(
            catalog,
            "skills",
            SpriteAtlasLoader.DefaultSkillImageName,
            issues);
        var (spells, spellsOnCooldown) = LoadAbility(
            catalog,
            "spells",
            SpriteAtlasLoader.DefaultSpellImageName,
            issues);

        ItemSpriteAtlasCollection? items = null;
        try
        {
            items = ItemSpriteAtlasLoader.Load(catalog);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           InvalidDataException or OverflowException)
        {
            issues.Add(new GameSpriteLoadIssue("items", exception));
        }

        return new GameSpriteData(
            skills,
            skillsOnCooldown,
            spells,
            spellsOnCooldown,
            items,
            new CreatureSpriteLoader(catalog),
            issues);
    }

    private static (SpriteAtlas? Normal, SpriteAtlas? Cooldown) LoadAbility(
        DatAssetCatalog catalog,
        string category,
        string imageName,
        ICollection<GameSpriteLoadIssue> issues)
    {
        try
        {
            var source = SpriteAtlasLoader.LoadNamed(catalog, imageName, SpriteAtlasLoader.DefaultGuiPaletteName);
            var grayscale = SpriteAtlasTransforms.Grayscale(source);
            var cooldown = SpriteAtlasTransforms.Tint(
                grayscale,
                CooldownTintRed,
                CooldownTintGreen,
                CooldownTintBlue);
            return (grayscale, cooldown);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                           InvalidDataException or OverflowException)
        {
            issues.Add(new GameSpriteLoadIssue(category, exception));
            return (null, null);
        }
    }
}
