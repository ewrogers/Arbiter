namespace Arbiter.Imaging.Sprites;

public sealed class SpriteAtlas
{
    private readonly byte[] _pixels;
    private readonly SpriteAtlasRegion[] _cells;
    private readonly SpriteAtlasRegion?[] _content;

    public string SourceImageName { get; }
    public string SourcePaletteName { get; }
    public int Width { get; }
    public int Height { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public int FrameCount => _cells.Length;
    public ReadOnlyMemory<byte> Pixels => _pixels;

    internal SpriteAtlas(
        string sourceImageName,
        string sourcePaletteName,
        int width,
        int height,
        int frameWidth,
        int frameHeight,
        byte[] pixels,
        SpriteAtlasRegion[] cells,
        SpriteAtlasRegion?[] content)
    {
        SourceImageName = sourceImageName;
        SourcePaletteName = sourcePaletteName;
        Width = width;
        Height = height;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        _pixels = pixels;
        _cells = cells;
        _content = content;
    }

    public bool TryGetFrame(int frameIndex, out SpriteAtlasRegion region)
    {
        region = default;
        if (frameIndex < 0 || frameIndex >= _content.Length || _content[frameIndex] is not { } content)
        {
            return false;
        }

        region = content;
        return true;
    }

    public bool TryResolveIcon(ushort icon, out int frameIndex, out SpriteAtlasRegion region)
    {
        var directIndex = (int)icon;
        if (TryGetFrame(directIndex, out region))
        {
            frameIndex = directIndex;
            return true;
        }

        var oneBasedIndex = directIndex - 1;
        if (TryGetFrame(oneBasedIndex, out region))
        {
            frameIndex = oneBasedIndex;
            return true;
        }

        frameIndex = -1;
        return false;
    }

    internal SpriteAtlas WithPixels(byte[] pixels)
    {
        if (pixels.Length != _pixels.Length)
        {
            throw new ArgumentException("Replacement atlas pixels must have the same length.", nameof(pixels));
        }

        return new SpriteAtlas(
            SourceImageName,
            SourcePaletteName,
            Width,
            Height,
            FrameWidth,
            FrameHeight,
            pixels,
            _cells,
            _content);
    }
}
