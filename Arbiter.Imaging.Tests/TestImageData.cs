using System.Buffers.Binary;
using System.Text;

namespace Arbiter.Imaging.Tests;

internal static class TestImageData
{
    private const int DatHeaderSize = sizeof(uint);
    private const int DatTableEntrySize = sizeof(uint) + 13;

    public static string WriteDat(string directory, string name, params (string Name, byte[] Data)[] assets)
    {
        var path = Path.Combine(directory, name);
        var tableEntryCount = assets.Length + 1;
        var dataOffset = DatHeaderSize + tableEntryCount * DatTableEntrySize;
        var bytes = new byte[dataOffset + assets.Sum(asset => asset.Data.Length)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)tableEntryCount);

        var currentOffset = dataOffset;
        for (var index = 0; index < assets.Length; index++)
        {
            WriteDatTableEntry(bytes, index, currentOffset, assets[index].Name);
            assets[index].Data.CopyTo(bytes, currentOffset);
            currentOffset += assets[index].Data.Length;
        }

        WriteDatTableEntry(bytes, assets.Length, currentOffset, string.Empty);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public static byte[] Epf(int width, int height, params byte[]?[] frames)
    {
        var pixelLength = frames.Sum(frame => frame?.Length ?? 0);
        var tableOffset = pixelLength;
        var bytes = new byte[12 + pixelLength + frames.Length * 16];
        BinaryPrimitives.WriteInt16LittleEndian(bytes, (short)frames.Length);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(2), (short)width);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(4), (short)height);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), (uint)tableOffset);

        var pixelOffset = 0;
        for (var index = 0; index < frames.Length; index++)
        {
            var frame = frames[index];
            var entryOffset = 12 + tableOffset + index * 16;
            if (frame is not null)
            {
                if (frame.Length != width * height)
                {
                    throw new ArgumentException("Test EPF frames must fill the declared canvas.");
                }

                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(entryOffset + 4), (short)height);
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(entryOffset + 6), (short)width);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 8), (uint)pixelOffset);
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 12), (uint)(pixelOffset + frame.Length));
                frame.CopyTo(bytes, 12 + pixelOffset);
                pixelOffset += frame.Length;
            }
        }

        return bytes;
    }

    public static byte[] Palette(params (byte Index, byte Red, byte Green, byte Blue)[] colors)
    {
        var bytes = new byte[256 * 3];
        foreach (var color in colors)
        {
            var offset = color.Index * 3;
            bytes[offset] = color.Red;
            bytes[offset + 1] = color.Green;
            bytes[offset + 2] = color.Blue;
        }

        return bytes;
    }

    public static byte[] Text(string value) => Encoding.ASCII.GetBytes(value);

    private static void WriteDatTableEntry(byte[] bytes, int index, int offset, string name)
    {
        var entryOffset = DatHeaderSize + index * DatTableEntrySize;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset), (uint)offset);
        var nameBytes = Encoding.ASCII.GetBytes(name);
        nameBytes.AsSpan(0, Math.Min(nameBytes.Length, 13)).CopyTo(bytes.AsSpan(entryOffset + 4, 13));
    }
}
