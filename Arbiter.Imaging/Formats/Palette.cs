using System.Buffers.Binary;

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
        if (bytes.Length == ByteLength)
        {
            return new Palette(bytes.ToArray());
        }

        return ParseRiff(bytes);
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

    private static Palette ParseRiff(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 12 || !bytes[..4].SequenceEqual("RIFF"u8) ||
            !bytes.Slice(8, 4).SequenceEqual("PAL "u8))
        {
            throw new InvalidDataException(
                $"A palette must be a {ByteLength}-byte RGB palette or a RIFF PAL file.");
        }

        var riffLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]) + 8);
        if (riffLength > bytes.Length)
        {
            throw new InvalidDataException("The RIFF palette exceeds the file bounds.");
        }

        var offset = 12;
        while (offset <= riffLength - 8)
        {
            var chunkName = bytes.Slice(offset, 4);
            var chunkLength = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 4)..]));
            var chunkOffset = offset + 8;
            if (chunkOffset > riffLength - chunkLength)
            {
                throw new InvalidDataException("A RIFF palette chunk exceeds the file bounds.");
            }

            if (chunkName.SequenceEqual("data"u8))
            {
                if (chunkLength < 4)
                {
                    throw new InvalidDataException("The RIFF palette data chunk is incomplete.");
                }

                var colorCount = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(chunkOffset + 2)..]);
                var requiredLength = checked(4 + colorCount * 4);
                if (colorCount > ColorCount || chunkLength < requiredLength)
                {
                    throw new InvalidDataException("The RIFF palette contains invalid color data.");
                }

                var colors = new byte[ByteLength];
                for (var index = 0; index < colorCount; index++)
                {
                    var sourceOffset = chunkOffset + 4 + index * 4;
                    var targetOffset = index * 3;
                    bytes.Slice(sourceOffset, 3).CopyTo(colors.AsSpan(targetOffset, 3));
                }

                return new Palette(colors);
            }

            offset = checked(chunkOffset + chunkLength + (chunkLength & 1));
        }

        throw new InvalidDataException("The RIFF palette does not contain a data chunk.");
    }
}

internal readonly record struct RgbColor(byte Red, byte Green, byte Blue);

internal static class PaletteDye
{
    public const int StartIndex = 98;
    public const int ColorCount = 6;
}
