using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Sprites;

internal readonly record struct PaletteBinding(Palette Palette, bool UseLuminanceAlpha);

internal static class SpriteAtlasBuilder
{
    public const int DefaultColumns = 16;

    public static SpriteAtlas Build(
        EpfFile epf,
        Func<int, PaletteBinding> paletteForFrame,
        string sourceImageName,
        string sourcePaletteName,
        int columns = DefaultColumns)
    {
        ArgumentNullException.ThrowIfNull(epf);
        ArgumentNullException.ThrowIfNull(paletteForFrame);
        if (epf.Frames.Count == 0)
        {
            throw new InvalidDataException("The EPF does not contain any frames.");
        }

        var frameWidth = epf.Frames.OfType<EpfFrame>()
            .Aggregate(epf.PixelWidth, (maximum, frame) => Math.Max(maximum, frame.Left + frame.Width));
        var frameHeight = epf.Frames.OfType<EpfFrame>()
            .Aggregate(epf.PixelHeight, (maximum, frame) => Math.Max(maximum, frame.Top + frame.Height));
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            throw new InvalidDataException("The EPF declares a zero-sized frame canvas.");
        }

        columns = Math.Clamp(columns, 1, epf.Frames.Count);
        var rows = (epf.Frames.Count + columns - 1) / columns;
        var atlasWidth = checked(frameWidth * columns);
        var atlasHeight = checked(frameHeight * rows);
        var pixels = new byte[checked(atlasWidth * atlasHeight * 4)];
        var cells = new SpriteAtlasRegion[epf.Frames.Count];
        var content = new SpriteAtlasRegion?[epf.Frames.Count];

        Span<byte> rgba = stackalloc byte[4];
        for (var frameIndex = 0; frameIndex < epf.Frames.Count; frameIndex++)
        {
            var cellX = frameIndex % columns * frameWidth;
            var cellY = frameIndex / columns * frameHeight;
            cells[frameIndex] = new SpriteAtlasRegion(cellX, cellY, frameWidth, frameHeight);
            if (epf.Frames[frameIndex] is not { } frame)
            {
                continue;
            }

            content[frameIndex] = new SpriteAtlasRegion(
                cellX + frame.Left,
                cellY + frame.Top,
                frame.Width,
                frame.Height);
            var palette = paletteForFrame(frameIndex);
            var framePixels = frame.Pixels.Span;
            for (var y = 0; y < frame.Height; y++)
            {
                for (var x = 0; x < frame.Width; x++)
                {
                    var sourceIndex = y * frame.Width + x;
                    palette.Palette.GetColor(framePixels[sourceIndex], palette.UseLuminanceAlpha, rgba);
                    var targetX = cellX + frame.Left + x;
                    var targetY = cellY + frame.Top + y;
                    var targetIndex = (targetY * atlasWidth + targetX) * 4;
                    rgba.CopyTo(pixels.AsSpan(targetIndex, 4));
                }
            }
        }

        return new SpriteAtlas(
            sourceImageName,
            sourcePaletteName,
            atlasWidth,
            atlasHeight,
            frameWidth,
            frameHeight,
            pixels,
            cells,
            content);
    }
}
