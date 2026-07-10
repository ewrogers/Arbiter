using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Sprites;

internal sealed class ItemPaletteLookup
{
    private readonly IReadOnlyDictionary<uint, Palette> _palettes;
    private readonly PaletteTable _table;
    private readonly DyeTable _dyeTable;
    private readonly IReadOnlyList<RgbColor>? _defaultDyeColors;

    public string SourceName { get; }

    public ItemPaletteLookup(
        IReadOnlyDictionary<uint, Palette> palettes,
        PaletteTable table,
        DyeTable dyeTable,
        IReadOnlyList<RgbColor>? defaultDyeColors,
        string sourceName)
    {
        _palettes = palettes;
        _table = table;
        _dyeTable = dyeTable;
        _defaultDyeColors = defaultDyeColors;
        SourceName = sourceName;
    }

    public PaletteBinding GetPalette(uint itemId, byte color)
    {
        var paletteId = _table.GetPaletteId(itemId);
        var useLuminanceAlpha = paletteId >= 1000;
        if (useLuminanceAlpha)
        {
            paletteId -= 1000;
        }

        if (!_palettes.TryGetValue(paletteId, out var palette))
        {
            throw new InvalidDataException($"Palette {paletteId} for item {itemId} was not found.");
        }

        if (color == 0 || _defaultDyeColors is null || _dyeTable.GetColors(color) is not { } dyeColors ||
            !palette.DyeRangeMatches(_defaultDyeColors))
        {
            return new PaletteBinding(palette, useLuminanceAlpha);
        }

        return new PaletteBinding(palette.WithDye(dyeColors), useLuminanceAlpha);
    }
}
