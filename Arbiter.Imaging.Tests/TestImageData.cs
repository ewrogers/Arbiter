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

    public static byte[] WindowsPalette(params (byte Index, byte Red, byte Green, byte Blue)[] colors)
    {
        var rgb = colors.ToDictionary(color => color.Index);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(16 + 256 * 4);
            writer.Write(Encoding.ASCII.GetBytes("PAL "));
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(4 + 256 * 4);
            writer.Write((ushort)0x0300);
            writer.Write((ushort)256);
            for (var index = 0; index < 256; index++)
            {
                var color = rgb.GetValueOrDefault((byte)index);
                writer.Write(color.Red);
                writer.Write(color.Green);
                writer.Write(color.Blue);
                writer.Write((byte)0);
            }
        }

        return stream.ToArray();
    }

    public static byte[] Mpf(
        int pixelWidth,
        int pixelHeight,
        IReadOnlyList<TestMpfFrame> frames,
        int paletteNumber = 0,
        byte walkFrameIndex = 0,
        byte walkFrameCount = 0,
        byte standingFrameIndex = 0,
        byte standingFrameCount = 0,
        byte optionalAnimationFrameCount = 0,
        byte optionalAnimationRatio = 0,
        byte attackFrameIndex = 0,
        byte attackFrameCount = 0,
        byte attack2FrameIndex = 0,
        byte attack2FrameCount = 0,
        byte attack3FrameIndex = 0,
        byte attack3FrameCount = 0,
        bool multipleAttacks = false,
        bool includePaletteRecord = true,
        bool unknownHeader = false,
        int unknownHeaderFlags = 0,
        int unknownHeaderWordCount = 0)
    {
        var dataLength = frames.Sum(frame => frame.Pixels.Length);
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, true))
        {
            if (unknownHeader)
            {
                writer.Write(-1);
                writer.Write(unknownHeaderFlags);
                if ((unknownHeaderFlags & 4) != 0)
                {
                    writer.Write(unknownHeaderWordCount);
                    for (var index = 0; index < unknownHeaderWordCount; index++)
                    {
                        writer.Write(0x10203040 + index);
                    }
                }
            }

            writer.Write(checked((byte)(frames.Count + (includePaletteRecord ? 1 : 0))));
            writer.Write(checked((short)pixelWidth));
            writer.Write(checked((short)pixelHeight));
            writer.Write(dataLength);
            writer.Write(walkFrameIndex);
            writer.Write(walkFrameCount);

            if (multipleAttacks)
            {
                writer.Write((short)-1);
                writer.Write(standingFrameIndex);
                writer.Write(standingFrameCount);
                writer.Write(optionalAnimationFrameCount);
                writer.Write(optionalAnimationRatio);
                writer.Write(attackFrameIndex);
                writer.Write(attackFrameCount);
                writer.Write(attack2FrameIndex);
                writer.Write(attack2FrameCount);
                writer.Write(attack3FrameIndex);
                writer.Write(attack3FrameCount);
            }
            else
            {
                writer.Write(attackFrameIndex);
                writer.Write(attackFrameCount);
                writer.Write(standingFrameIndex);
                writer.Write(standingFrameCount);
                writer.Write(optionalAnimationFrameCount);
                writer.Write(optionalAnimationRatio);
            }

            var startAddress = 0;
            foreach (var frame in frames)
            {
                writer.Write(frame.Left);
                writer.Write(frame.Top);
                writer.Write(checked((short)(frame.Left + frame.Width)));
                writer.Write(checked((short)(frame.Top + frame.Height)));
                writer.Write(frame.CenterX);
                writer.Write(frame.CenterY);
                writer.Write(startAddress);
                startAddress += frame.Pixels.Length;
            }

            if (includePaletteRecord)
            {
                for (var index = 0; index < 6; index++)
                {
                    writer.Write((short)-1);
                }

                writer.Write(paletteNumber);
            }

            foreach (var frame in frames)
            {
                writer.Write(frame.Pixels);
            }
        }

        return stream.ToArray();
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

internal readonly record struct TestMpfFrame(
    short Left,
    short Top,
    short Width,
    short Height,
    short CenterX,
    short CenterY,
    byte[] Pixels);
