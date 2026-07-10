using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Sprites;

public sealed class ItemSpriteAtlas
{
    private readonly EpfFile _epf;
    private readonly ItemPaletteLookup _paletteLookup;
    private readonly uint _imageIdentifier;
    private readonly string _sourceImageName;

    public const uint FramesPerImage = 266;

    public uint FirstItemId { get; }
    public uint LastItemId { get; }
    public SpriteAtlas BaseAtlas { get; }

    internal ItemSpriteAtlas(
        uint firstItemId,
        uint lastItemId,
        SpriteAtlas baseAtlas,
        EpfFile epf,
        ItemPaletteLookup paletteLookup,
        uint imageIdentifier,
        string sourceImageName)
    {
        FirstItemId = firstItemId;
        LastItemId = lastItemId;
        BaseAtlas = baseAtlas;
        _epf = epf;
        _paletteLookup = paletteLookup;
        _imageIdentifier = imageIdentifier;
        _sourceImageName = sourceImageName;
    }

    public SpriteAtlas BuildColorVariant(byte color) =>
        color == 0
            ? BaseAtlas
            : ItemSpriteAtlasLoader.BuildAtlas(_epf, _paletteLookup, _imageIdentifier, _sourceImageName, color);
}
