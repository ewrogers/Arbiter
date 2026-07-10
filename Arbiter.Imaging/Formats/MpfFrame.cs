namespace Arbiter.Imaging.Formats;

public sealed class MpfFrame
{
    private readonly byte[] _pixels;

    public int Left { get; }
    public int Top { get; }
    public int Width { get; }
    public int Height { get; }
    public int CenterX { get; }
    public int CenterY { get; }
    public ReadOnlyMemory<byte> Pixels => _pixels;

    internal MpfFrame(
        int left,
        int top,
        int width,
        int height,
        int centerX,
        int centerY,
        byte[] pixels)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        CenterX = centerX;
        CenterY = centerY;
        _pixels = pixels;
    }
}
