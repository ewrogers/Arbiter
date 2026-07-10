using Arbiter.IO.Assets;
using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Sprites;

public static class ItemSpriteAtlasLoader
{
    public const string DefaultPaletteTableName = "itempal.tbl";
    public const string DefaultDyeTableName = "color0.tbl";

    public static ItemSpriteAtlasCollection Load(DatAssetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var imageNames = GetNumberedNames(catalog, "item", ".epf");
        if (imageNames.Count == 0)
        {
            throw new FileNotFoundException("No item EPF assets were found.", "item*.epf");
        }

        var paletteLookup = LoadPaletteLookup(catalog);
        var atlases = new List<ItemSpriteAtlas>(imageNames.Count);
        foreach (var imageName in imageNames)
        {
            var imageIdentifier = GetNumericIdentifier(imageName, "item", ".epf")!.Value;
            if (imageIdentifier == 0)
            {
                continue;
            }

            var firstItemId = checked((imageIdentifier - 1) * ItemSpriteAtlas.FramesPerImage + 1);
            var (epf, sourceImageName) = LoadBestItemImage(catalog, imageName);
            var atlas = BuildAtlas(epf, paletteLookup, imageIdentifier, sourceImageName, 0);
            var lastItemId = checked(firstItemId + (uint)atlas.FrameCount - 1);
            atlases.Add(new ItemSpriteAtlas(
                firstItemId,
                lastItemId,
                atlas,
                epf,
                paletteLookup,
                imageIdentifier,
                sourceImageName));
        }

        return new ItemSpriteAtlasCollection(atlases);
    }

    internal static SpriteAtlas BuildAtlas(
        EpfFile epf,
        ItemPaletteLookup paletteLookup,
        uint imageIdentifier,
        string sourceImageName,
        byte color)
    {
        var baseIdentifier = checked(imageIdentifier - 1);
        return SpriteAtlasBuilder.Build(
            epf,
            frameIndex =>
            {
                var itemId = checked(baseIdentifier * ItemSpriteAtlas.FramesPerImage + (uint)frameIndex + 1);
                return paletteLookup.GetPalette(itemId, color);
            },
            sourceImageName,
            color == 0 ? paletteLookup.SourceName : $"{paletteLookup.SourceName} + {DefaultDyeTableName}[{color}]");
    }

    private static ItemPaletteLookup LoadPaletteLookup(DatAssetCatalog catalog)
    {
        var tableNames = catalog.Names
            .Where(name => name.StartsWith("itempal", StringComparison.OrdinalIgnoreCase) &&
                           name.EndsWith(".tbl", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (tableNames.Length == 0)
        {
            throw new FileNotFoundException("No item palette table was found.", "itempal*.tbl");
        }

        var paletteNames = GetNumberedNames(catalog, "item", ".pal");
        if (paletteNames.Count == 0)
        {
            throw new FileNotFoundException("No item palettes were found.", "item*.pal");
        }

        var table = new PaletteTable();
        foreach (var tableName in tableNames)
        {
            catalog.TryGet(tableName, out var asset);
            using var stream = asset!.OpenRead();
            table.Merge(stream);
        }

        var palettes = new Dictionary<uint, Palette>();
        foreach (var paletteName in paletteNames)
        {
            catalog.TryGet(paletteName, out var asset);
            using var stream = asset!.OpenRead();
            palettes[GetNumericIdentifier(paletteName, "item", ".pal")!.Value] = Palette.Load(stream);
        }

        var dyeTable = DyeTable.Empty;
        IReadOnlyList<RgbColor>? defaultDyeColors = null;
        if (catalog.TryGet(DefaultDyeTableName, out var dyeAsset))
        {
            using var stream = dyeAsset.OpenRead();
            dyeTable = DyeTable.Parse(stream);
            defaultDyeColors = dyeTable.GetColors(0);
        }

        return new ItemPaletteLookup(
            palettes,
            table,
            dyeTable,
            defaultDyeColors,
            string.Join(" + ", tableNames.Append("item*.pal")));
    }

    private static (EpfFile Epf, string SourceName) LoadBestItemImage(DatAssetCatalog catalog, string imageName)
    {
        var parsed = new List<(DatAsset Asset, EpfFile Epf)>();
        Exception? firstError = null;
        foreach (var asset in catalog.GetAll(imageName))
        {
            try
            {
                using var stream = asset.OpenRead();
                parsed.Add((asset, EpfFile.Load(stream)));
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                firstError ??= exception;
            }
        }

        var best = parsed
            .OrderByDescending(candidate =>
                string.Equals(candidate.Asset.ArchiveName, "Legend.dat", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(candidate => candidate.Epf.Frames.Count)
            .ThenByDescending(candidate => candidate.Asset.Length)
            .FirstOrDefault();
        if (best.Asset is null)
        {
            throw firstError ?? new FileNotFoundException($"Asset '{imageName}' was not found.", imageName);
        }

        return (best.Epf, $"{best.Asset.Name} ({best.Asset.ArchiveName})");
    }

    private static List<string> GetNumberedNames(DatAssetCatalog catalog, string prefix, string suffix) =>
        catalog.Names
            .Select(name => (Name: name, Identifier: GetNumericIdentifier(name, prefix, suffix)))
            .Where(value => value.Identifier.HasValue)
            .OrderBy(value => value.Identifier)
            .ThenBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(value => value.Name)
            .ToList();

    private static uint? GetNumericIdentifier(string name, string prefix, string suffix)
    {
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var identifier = name[prefix.Length..^suffix.Length];
        return uint.TryParse(identifier, out var value) ? value : null;
    }
}
