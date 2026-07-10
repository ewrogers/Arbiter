namespace Arbiter.Imaging.Formats;

public sealed class Palette
{
    public const int ColorCount = 256;
    public const int ByteLength = ColorCount * 3;

    private readonly byte[] _colors;

    private Palette(byte[] colors)
    {
        _colors = colors;
    }

    public static Palette Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Parse(buffer.ToArray());
    }

    public static Palette Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteLength)
        {
            throw new InvalidDataException($"A palette must contain exactly {ByteLength} bytes.");
        }

        return new Palette(bytes.ToArray());
    }

    public void GetColor(byte index, bool useLuminanceAlpha, Span<byte> rgba)
    {
        if (rgba.Length < 4)
        {
            throw new ArgumentException("The destination must contain at least four bytes.", nameof(rgba));
        }

        if (index == 0)
        {
            rgba[..4].Clear();
            return;
        }

        var offset = index * 3;
        var red = _colors[offset];
        var green = _colors[offset + 1];
        var blue = _colors[offset + 2];
        rgba[0] = red;
        rgba[1] = green;
        rgba[2] = blue;
        rgba[3] = useLuminanceAlpha ? GetLuminanceAlpha(red, green, blue) : byte.MaxValue;
    }

    internal bool DyeRangeMatches(IReadOnlyList<RgbColor> colors)
    {
        for (var index = 0; index < colors.Count; index++)
        {
            var offset = (PaletteDye.StartIndex + index) * 3;
            if (_colors[offset] != colors[index].Red ||
                _colors[offset + 1] != colors[index].Green ||
                _colors[offset + 2] != colors[index].Blue)
            {
                return false;
            }
        }

        return true;
    }

    internal Palette WithDye(IReadOnlyList<RgbColor> colors)
    {
        var dyed = (byte[])_colors.Clone();
        for (var index = 0; index < colors.Count; index++)
        {
            var offset = (PaletteDye.StartIndex + index) * 3;
            dyed[offset] = colors[index].Red;
            dyed[offset + 1] = colors[index].Green;
            dyed[offset + 2] = colors[index].Blue;
        }

        return new Palette(dyed);
    }

    private static byte GetLuminanceAlpha(byte red, byte green, byte blue)
    {
        const double gamma = 2.0;
        var linearRed = Math.Pow(red / 255.0, gamma);
        var linearGreen = Math.Pow(green / 255.0, gamma);
        var linearBlue = Math.Pow(blue / 255.0, gamma);
        var linearLuminance = 0.299 * linearRed + 0.587 * linearGreen + 0.114 * linearBlue;
        var luminance = Math.Pow(linearLuminance, 1.0 / gamma) * 255.0;
        return (byte)Math.Clamp(Math.Round(luminance), byte.MinValue, byte.MaxValue);
    }
}

internal readonly record struct RgbColor(byte Red, byte Green, byte Blue);

internal static class PaletteDye
{
    public const int StartIndex = 98;
    public const int ColorCount = 6;
}
