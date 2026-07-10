using Arbiter.IO.Assets;
using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Sprites;

public static class SpriteAtlasLoader
{
    public const string DefaultSkillImageName = "skill001.epf";
    public const string DefaultSpellImageName = "spell001.epf";
    public const string DefaultGuiPaletteName = "gui06.pal";

    public static SpriteAtlas LoadNamed(DatAssetCatalog catalog, string imageName, string paletteName)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (!catalog.TryGet(imageName, out var imageAsset))
        {
            throw new FileNotFoundException($"Asset '{imageName}' was not found.", imageName);
        }

        if (!catalog.TryGet(paletteName, out var paletteAsset))
        {
            throw new FileNotFoundException($"Asset '{paletteName}' was not found.", paletteName);
        }

        using var imageStream = imageAsset.OpenRead();
        using var paletteStream = paletteAsset.OpenRead();
        var epf = EpfFile.Load(imageStream);
        var palette = Palette.Load(paletteStream);
        return SpriteAtlasBuilder.Build(
            epf,
            _ => new PaletteBinding(palette, false),
            imageAsset.Name,
            paletteAsset.Name);
    }
}
