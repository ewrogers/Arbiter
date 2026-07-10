using System.Buffers.Binary;

namespace Arbiter.Imaging.Formats;

// The MPF layout is adapted from DALib's MIT-licensed MpfFile and MpfView implementations.
public sealed class MpfFile
{
    private const int FrameTableEntrySize = 16;
    private const int StaticNoIdleIntervalMs = 10_000;
    private const int DefaultIdleIntervalMs = 300;
    private const int MinimumNormalIdleIntervalMs = 100;

    public MpfHeaderType HeaderType { get; }
    public MpfFormatType FormatType { get; }
    public MpfIdleType IdleType { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public int PaletteNumber { get; }
    public int WalkFrameIndex { get; }
    public int WalkFrameCount { get; }
    public int StandingFrameIndex { get; }
    public int StandingFrameCount { get; }
    public int OptionalAnimationFrameCount { get; }
    public int OptionalAnimationProbability { get; }
    public int AnimationIntervalMs { get; }
    public int AttackFrameIndex { get; }
    public int AttackFrameCount { get; }
    public int Attack2FrameIndex { get; }
    public int Attack2FrameCount { get; }
    public int Attack3FrameIndex { get; }
    public int Attack3FrameCount { get; }
    public ReadOnlyMemory<byte> UnknownHeaderBytes { get; }
    public IReadOnlyList<MpfFrame?> Frames { get; }

    private MpfFile(
        MpfHeaderType headerType,
        MpfFormatType formatType,
        int pixelWidth,
        int pixelHeight,
        int paletteNumber,
        int walkFrameIndex,
        int walkFrameCount,
        int standingFrameIndex,
        int standingFrameCount,
        int optionalAnimationFrameCount,
        int rawOptionalAnimationRatio,
        int attackFrameIndex,
        int attackFrameCount,
        int attack2FrameIndex,
        int attack2FrameCount,
        int attack3FrameIndex,
        int attack3FrameCount,
        byte[] unknownHeaderBytes,
        IReadOnlyList<MpfFrame?> frames)
    {
        HeaderType = headerType;
        FormatType = formatType;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
        PaletteNumber = paletteNumber;
        WalkFrameIndex = walkFrameIndex;
        WalkFrameCount = walkFrameCount;
        StandingFrameIndex = standingFrameIndex;
        StandingFrameCount = standingFrameCount;
        OptionalAnimationFrameCount = optionalAnimationFrameCount;
        AttackFrameIndex = attackFrameIndex;
        AttackFrameCount = attackFrameCount;
        Attack2FrameIndex = attack2FrameIndex;
        Attack2FrameCount = attack2FrameCount;
        Attack3FrameIndex = attack3FrameIndex;
        Attack3FrameCount = attack3FrameCount;
        UnknownHeaderBytes = unknownHeaderBytes;
        Frames = frames;

        IdleType = DetectIdleType(standingFrameCount, optionalAnimationFrameCount);
        switch (IdleType)
        {
            case MpfIdleType.StaticNoIdle:
                AnimationIntervalMs = StaticNoIdleIntervalMs;
                break;
            case MpfIdleType.NormalIdle:
                AnimationIntervalMs = rawOptionalAnimationRatio > 0
                    ? Math.Max(MinimumNormalIdleIntervalMs, rawOptionalAnimationRatio * 100)
                    : DefaultIdleIntervalMs;
                break;
            case MpfIdleType.NormalPlusOptional:
                AnimationIntervalMs = DefaultIdleIntervalMs;
                OptionalAnimationProbability = rawOptionalAnimationRatio;
                break;
        }
    }

    public static MpfFile Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return Parse(buffer.ToArray());
    }

    public static MpfFile Parse(ReadOnlySpan<byte> bytes)
    {
        var offset = 0;
        var headerType = MpfHeaderType.None;
        byte[] unknownHeaderBytes = [];
        if (bytes.Length >= sizeof(int) && ReadInt32(bytes, 0) == (int)MpfHeaderType.Unknown)
        {
            headerType = MpfHeaderType.Unknown;
            offset += sizeof(int);
            var unknownHeaderStart = offset;
            var flags = ReadInt32(bytes, offset);
            offset += sizeof(int);
            if ((flags & 4) != 0)
            {
                var count = ReadNonNegativeInt32(bytes, offset, "extended header count");
                offset += sizeof(int);
                var extraLength = checked(count * sizeof(int));
                EnsureAvailable(bytes, offset, extraLength, "extended header data");
                offset += extraLength;
            }

            unknownHeaderBytes = bytes[unknownHeaderStart..offset].ToArray();
        }

        var declaredFrameCount = ReadByte(bytes, ref offset, "frame count");
        var pixelWidth = ReadNonNegativeInt16(bytes, ref offset, "pixel width");
        var pixelHeight = ReadNonNegativeInt16(bytes, ref offset, "pixel height");
        var dataLength = ReadNonNegativeInt32(bytes, offset, "data length");
        offset += sizeof(int);
        var walkFrameIndex = ReadByte(bytes, ref offset, "walk frame index");
        var walkFrameCount = ReadByte(bytes, ref offset, "walk frame count");

        var formatMarkerOffset = offset;
        var formatMarker = ReadInt16(bytes, ref offset, "format type");
        var formatType = formatMarker == (short)MpfFormatType.MultipleAttacks
            ? MpfFormatType.MultipleAttacks
            : MpfFormatType.SingleAttack;

        int standingFrameIndex;
        int standingFrameCount;
        int optionalAnimationFrameCount;
        int rawOptionalAnimationRatio;
        int attackFrameIndex;
        int attackFrameCount;
        var attack2FrameIndex = 0;
        var attack2FrameCount = 0;
        var attack3FrameIndex = 0;
        var attack3FrameCount = 0;

        if (formatType == MpfFormatType.MultipleAttacks)
        {
            standingFrameIndex = ReadByte(bytes, ref offset, "standing frame index");
            standingFrameCount = ReadByte(bytes, ref offset, "standing frame count");
            optionalAnimationFrameCount = ReadByte(bytes, ref offset, "optional animation frame count");
            rawOptionalAnimationRatio = ReadByte(bytes, ref offset, "optional animation ratio");
            attackFrameIndex = ReadByte(bytes, ref offset, "attack frame index");
            attackFrameCount = ReadByte(bytes, ref offset, "attack frame count");
            attack2FrameIndex = ReadByte(bytes, ref offset, "second attack frame index");
            attack2FrameCount = ReadByte(bytes, ref offset, "second attack frame count");
            attack3FrameIndex = ReadByte(bytes, ref offset, "third attack frame index");
            attack3FrameCount = ReadByte(bytes, ref offset, "third attack frame count");
        }
        else
        {
            offset = formatMarkerOffset;
            attackFrameIndex = ReadByte(bytes, ref offset, "attack frame index");
            attackFrameCount = ReadByte(bytes, ref offset, "attack frame count");
            standingFrameIndex = ReadByte(bytes, ref offset, "standing frame index");
            standingFrameCount = ReadByte(bytes, ref offset, "standing frame count");
            optionalAnimationFrameCount = ReadByte(bytes, ref offset, "optional animation frame count");
            rawOptionalAnimationRatio = ReadByte(bytes, ref offset, "optional animation ratio");
        }

        var dataStart = bytes.Length - dataLength;
        if (dataStart < offset)
        {
            throw new InvalidDataException("The MPF data segment overlaps its header.");
        }

        var tableLength = checked(declaredFrameCount * FrameTableEntrySize);
        if (offset > dataStart - tableLength)
        {
            throw new InvalidDataException("The MPF frame table exceeds the file bounds.");
        }

        var paletteNumber = 0;
        var frames = new List<MpfFrame?>(declaredFrameCount);
        for (var frameIndex = 0; frameIndex < declaredFrameCount; frameIndex++)
        {
            var entryOffset = checked(offset + frameIndex * FrameTableEntrySize);
            var left = ReadInt16(bytes, entryOffset);
            var top = ReadInt16(bytes, entryOffset + 2);
            var right = ReadInt16(bytes, entryOffset + 4);
            var bottom = ReadInt16(bytes, entryOffset + 6);
            var centerX = ReadInt16(bytes, entryOffset + 8);
            var centerY = ReadInt16(bytes, entryOffset + 10);
            var startAddress = ReadInt32(bytes, entryOffset + 12);

            if (left == -1 && top == -1)
            {
                paletteNumber = startAddress;
                continue;
            }

            var width = right - left;
            var height = bottom - top;
            if (width == 0 || height == 0)
            {
                frames.Add(null);
                continue;
            }

            if (left < 0 || top < 0 || width < 0 || height < 0)
            {
                throw new InvalidDataException($"MPF frame {frameIndex} has invalid dimensions.");
            }

            var frameDataLength = checked(width * height);
            if (startAddress < 0 || startAddress > dataLength - frameDataLength)
            {
                throw new InvalidDataException($"MPF frame {frameIndex} data exceeds the file bounds.");
            }

            frames.Add(new MpfFrame(
                left,
                top,
                width,
                height,
                centerX,
                centerY,
                bytes.Slice(dataStart + startAddress, frameDataLength).ToArray()));
        }

        return new MpfFile(
            headerType,
            formatType,
            pixelWidth,
            pixelHeight,
            paletteNumber,
            walkFrameIndex,
            walkFrameCount,
            standingFrameIndex,
            standingFrameCount,
            optionalAnimationFrameCount,
            rawOptionalAnimationRatio,
            attackFrameIndex,
            attackFrameCount,
            attack2FrameIndex,
            attack2FrameCount,
            attack3FrameIndex,
            attack3FrameCount,
            unknownHeaderBytes,
            frames);
    }

    public static MpfIdleType DetectIdleType(int standingFrameCount, int optionalAnimationFrameCount)
    {
        if (optionalAnimationFrameCount == 0)
        {
            return MpfIdleType.StaticNoIdle;
        }

        return standingFrameCount == 0 || standingFrameCount == optionalAnimationFrameCount
            ? MpfIdleType.NormalIdle
            : MpfIdleType.NormalPlusOptional;
    }

    private static byte ReadByte(ReadOnlySpan<byte> bytes, ref int offset, string fieldName)
    {
        EnsureAvailable(bytes, offset, sizeof(byte), fieldName);
        return bytes[offset++];
    }

    private static int ReadNonNegativeInt16(ReadOnlySpan<byte> bytes, ref int offset, string fieldName)
    {
        var value = ReadInt16(bytes, ref offset, fieldName);
        return value < 0
            ? throw new InvalidDataException($"The MPF {fieldName} cannot be negative.")
            : value;
    }

    private static int ReadNonNegativeInt32(ReadOnlySpan<byte> bytes, int offset, string fieldName)
    {
        var value = ReadInt32(bytes, offset);
        return value < 0
            ? throw new InvalidDataException($"The MPF {fieldName} cannot be negative.")
            : value;
    }

    private static short ReadInt16(ReadOnlySpan<byte> bytes, ref int offset, string fieldName)
    {
        EnsureAvailable(bytes, offset, sizeof(short), fieldName);
        var value = BinaryPrimitives.ReadInt16LittleEndian(bytes[offset..]);
        offset += sizeof(short);
        return value;
    }

    private static short ReadInt16(ReadOnlySpan<byte> bytes, int offset)
    {
        EnsureAvailable(bytes, offset, sizeof(short), "frame table value");
        return BinaryPrimitives.ReadInt16LittleEndian(bytes[offset..]);
    }

    private static int ReadInt32(ReadOnlySpan<byte> bytes, int offset)
    {
        EnsureAvailable(bytes, offset, sizeof(int), "Int32 value");
        return BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]);
    }

    private static void EnsureAvailable(ReadOnlySpan<byte> bytes, int offset, int length, string fieldName)
    {
        if (offset < 0 || length < 0 || offset > bytes.Length - length)
        {
            throw new InvalidDataException($"The MPF is missing its {fieldName}.");
        }
    }
}
