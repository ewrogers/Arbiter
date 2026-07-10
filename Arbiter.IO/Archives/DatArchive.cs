using System.Buffers.Binary;
using System.Text;

namespace Arbiter.IO.Archives;

public sealed class DatArchive
{
    private const int HeaderSize = sizeof(uint);
    private const int TableEntrySize = sizeof(uint) + NameLength;
    private const int NameLength = 13;

    private readonly IReadOnlyList<DatArchiveEntry> _entries;

    public string FilePath { get; }
    public string Name => Path.GetFileName(FilePath);
    public IReadOnlyList<DatArchiveEntry> Entries => _entries;

    private DatArchive(string filePath, IReadOnlyList<DatArchiveEntry> entries)
    {
        FilePath = filePath;
        _entries = entries;
    }

    public static DatArchive Open(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < HeaderSize)
        {
            throw new InvalidDataException($"Archive '{filePath}' is too small for a header.");
        }

        Span<byte> header = stackalloc byte[HeaderSize];
        stream.ReadExactly(header);
        var tableEntryCount = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (tableEntryCount == 0)
        {
            throw new InvalidDataException($"Archive '{filePath}' does not contain an end marker.");
        }

        var tableLength = checked((long)tableEntryCount * TableEntrySize);
        var tableEnd = checked(HeaderSize + tableLength);
        if (tableEnd > stream.Length)
        {
            throw new InvalidDataException($"Archive '{filePath}' table exceeds the file bounds.");
        }

        var tableBytes = new byte[checked((int)tableLength)];
        stream.ReadExactly(tableBytes);

        var entries = new List<DatArchiveEntry>(checked((int)tableEntryCount - 1));
        for (var index = 0; index < tableEntryCount - 1; index++)
        {
            var currentOffset = ReadOffset(tableBytes, index);
            var nextOffset = ReadOffset(tableBytes, index + 1);
            if (currentOffset < tableEnd || nextOffset < currentOffset || nextOffset > stream.Length)
            {
                throw new InvalidDataException(
                    $"Archive '{filePath}' entry {index} has an invalid range {currentOffset}..{nextOffset}.");
            }

            var nameOffset = checked((int)index * TableEntrySize + sizeof(uint));
            var nameBytes = tableBytes.AsSpan(nameOffset, NameLength);
            var terminator = nameBytes.IndexOf((byte)0);
            if (terminator >= 0)
            {
                nameBytes = nameBytes[..terminator];
            }

            var name = Encoding.ASCII.GetString(nameBytes);
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidDataException($"Archive '{filePath}' entry {index} has an empty name.");
            }

            entries.Add(new DatArchiveEntry(name, currentOffset, nextOffset - currentOffset));
        }

        return new DatArchive(Path.GetFullPath(filePath), entries);
    }

    public Stream OpenRead(DatArchiveEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (!_entries.Contains(entry))
        {
            throw new ArgumentException("The entry does not belong to this archive.", nameof(entry));
        }

        return new ArchiveEntryStream(FilePath, entry.Offset, entry.Length);
    }

    private static long ReadOffset(ReadOnlySpan<byte> table, int index)
    {
        var offset = checked((int)index * TableEntrySize);
        return BinaryPrimitives.ReadUInt32LittleEndian(table.Slice(offset, sizeof(uint)));
    }
}
