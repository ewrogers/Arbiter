namespace Arbiter.Imaging.Formats;

public sealed class EpfFrame
{
    private readonly byte[] _pixels;

    public int Left { get; }
    public int Top { get; }
    public int Width { get; }
    public int Height { get; }
    public ReadOnlyMemory<byte> Pixels => _pixels;

    internal EpfFrame(int left, int top, int width, int height, byte[] pixels)
    {
        Left = left;
        Top = top;
        Width = width;
        Height = height;
        _pixels = pixels;
    }
}
