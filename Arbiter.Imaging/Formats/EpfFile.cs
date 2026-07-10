using System.Buffers.Binary;

namespace Arbiter.Imaging.Formats;

public sealed class EpfFile
{
    private const int HeaderSize = 12;
    private const int TableEntrySize = 16;

    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public IReadOnlyList<EpfFrame?> Frames { get; }

    private EpfFile(int pixelWidth, int pixelHeight, IReadOnlyList<EpfFrame?> frames)
    {
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        Frames = frames;
    }

    public static EpfFile Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Parse(buffer.ToArray());
    }

    public static EpfFile Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderSize)
        {
            throw new InvalidDataException("The EPF is too small for a header.");
        }

        var frameCount = ReadNonNegativeInt16(bytes, 0, "frame count");
        var pixelWidth = ReadNonNegativeInt16(bytes, 2, "pixel width");
        var pixelHeight = ReadNonNegativeInt16(bytes, 4, "pixel height");
        var tableOffset = checked((int)ReadUInt32(bytes, 8));
        var data = bytes[HeaderSize..];
        var tableLength = checked(frameCount * TableEntrySize);
        if (tableOffset > data.Length - tableLength)
        {
            throw new InvalidDataException("The EPF frame table exceeds the file bounds.");
        }

        var frames = new List<EpfFrame?>(frameCount);
        for (var index = 0; index < frameCount; index++)
        {
            var entryOffset = checked(tableOffset + index * TableEntrySize);
            var top = ReadInt16(data, entryOffset);
            var left = ReadInt16(data, entryOffset + 2);
            var bottom = ReadInt16(data, entryOffset + 4);
            var right = ReadInt16(data, entryOffset + 6);
            var start = checked((int)ReadUInt32(data, entryOffset + 8));
            var end = checked((int)ReadUInt32(data, entryOffset + 12));

            var width = right - left;
            var height = bottom - top;
            if (width == 0 || height == 0)
            {
                frames.Add(null);
                continue;
            }

            if (left < 0 || top < 0 || width < 0 || height < 0)
            {
                throw new InvalidDataException($"EPF frame {index} has invalid dimensions.");
            }

            var expectedLength = checked(width * height);
            var declaredLength = end >= start ? end - start : 0;
            var dataLength = declaredLength == expectedLength ? declaredLength : tableOffset - start;
            if (start < 0 || dataLength < expectedLength || start > data.Length - dataLength)
            {
                throw new InvalidDataException($"EPF frame {index} data exceeds the file bounds.");
            }

            frames.Add(new EpfFrame(left, top, width, height, data.Slice(start, dataLength).ToArray()));
        }

        return new EpfFile(pixelWidth, pixelHeight, frames);
    }

    private static int ReadNonNegativeInt16(ReadOnlySpan<byte> bytes, int offset, string fieldName)
    {
        var value = ReadInt16(bytes, offset);
        return value < 0
            ? throw new InvalidDataException($"The EPF {fieldName} cannot be negative.")
            : value;
    }

    private static short ReadInt16(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset < 0 || offset > bytes.Length - sizeof(short))
        {
            throw new InvalidDataException($"The EPF is missing an Int16 at offset {offset}.");
        }

        return BinaryPrimitives.ReadInt16LittleEndian(bytes[offset..]);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset)
    {
        if (offset < 0 || offset > bytes.Length - sizeof(uint))
        {
            throw new InvalidDataException($"The EPF is missing a UInt32 at offset {offset}.");
        }

        return BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    }
}
