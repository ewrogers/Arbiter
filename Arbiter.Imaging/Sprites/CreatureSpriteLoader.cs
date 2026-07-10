using Arbiter.IO.Assets;
using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Sprites;

public sealed class CreatureSpriteLoader
{
    private const string ImagePrefix = "mns";
    private const string ImageSuffix = ".mpf";
    private const string PaletteSuffix = ".pal";

    private readonly DatAssetCatalog _catalog;

    public CreatureSpriteLoader(DatAssetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
    }

    public SpriteAtlas? LoadPreview(ushort sprite)
    {
        if (sprite == 0)
        {
            return null;
        }

        var imageName = $"{ImagePrefix}{sprite:D3}{ImageSuffix}";
        if (!_catalog.TryGet(imageName, out var imageAsset))
        {
            return null;
        }

        using var imageStream = imageAsset.OpenRead();
        var mpf = MpfFile.Load(imageStream);
        var (frame, flipHorizontally) = GetSouthFacingPreviewFrame(mpf);
        if (frame is null)
        {
            return null;
        }

        var paletteName = $"{ImagePrefix}{mpf.PaletteNumber:D3}{PaletteSuffix}";
        if (!_catalog.TryGet(paletteName, out var paletteAsset))
        {
            throw new FileNotFoundException($"Asset '{paletteName}' was not found.", paletteName);
        }

        using var paletteStream = paletteAsset.OpenRead();
        var palette = Palette.Load(paletteStream);
        return BuildPreview(
            frame,
            palette,
            $"{imageAsset.Name} ({imageAsset.ArchiveName})",
            $"{paletteAsset.Name} ({paletteAsset.ArchiveName})",
            flipHorizontally);
    }

    private static (MpfFrame? Frame, bool FlipHorizontally) GetSouthFacingPreviewFrame(MpfFile mpf)
    {
        var baseIndex = mpf.OptionalAnimationFrameCount == 0
            ? mpf.WalkFrameIndex
            : mpf.StandingFrameIndex;
        var directionOffset = mpf.OptionalAnimationFrameCount == 0
            ? mpf.WalkFrameCount
            : mpf.OptionalAnimationFrameCount;
        if (baseIndex + directionOffset >= mpf.Frames.Count)
        {
            directionOffset = 0;
        }

        if (GetFrame(mpf, baseIndex + directionOffset) is { } southFacing)
        {
            return (southFacing, true);
        }

        if (mpf.StandingFrameCount > 0 && GetFrame(mpf, mpf.StandingFrameIndex) is { } standing)
        {
            return (standing, true);
        }

        if (mpf.WalkFrameCount > 0 && GetFrame(mpf, mpf.WalkFrameIndex) is { } walking)
        {
            return (walking, true);
        }

        return (mpf.Frames.FirstOrDefault(frame => frame is not null), true);
    }

    private static MpfFrame? GetFrame(MpfFile mpf, int index)
    {
        return index >= 0 && index < mpf.Frames.Count ? mpf.Frames[index] : null;
    }

    private static SpriteAtlas BuildPreview(
        MpfFrame frame,
        Palette palette,
        string sourceImageName,
        string sourcePaletteName,
        bool flipHorizontally)
    {
        var pixels = new byte[checked(frame.Width * frame.Height * 4)];
        var indexedPixels = frame.Pixels.Span;
        Span<byte> rgba = stackalloc byte[4];
        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width; x++)
            {
                var sourceX = flipHorizontally ? frame.Width - x - 1 : x;
                var sourceIndex = y * frame.Width + sourceX;
                var targetIndex = (y * frame.Width + x) * 4;
                palette.GetColor(indexedPixels[sourceIndex], false, rgba);
                rgba.CopyTo(pixels.AsSpan(targetIndex, 4));
            }
        }

        var region = new SpriteAtlasRegion(0, 0, frame.Width, frame.Height);
        return new SpriteAtlas(
            sourceImageName,
            sourcePaletteName,
            frame.Width,
            frame.Height,
            frame.Width,
            frame.Height,
            pixels,
            [region],
            [region]);
    }
}
