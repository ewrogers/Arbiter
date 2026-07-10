using System.Buffers.Binary;
using System.Text;

namespace Arbiter.IO.Tests.Archives;

internal static class MockDatArchive
{
    private const int HeaderSize = sizeof(uint);
    private const int TableEntrySize = sizeof(uint) + 13;

    public static string Write(string directory, string fileName, params (string Name, byte[] Data)[] entries)
    {
        var path = Path.Combine(directory, fileName);
        var tableEntryCount = entries.Length + 1;
        var dataOffset = HeaderSize + tableEntryCount * TableEntrySize;
        var bytes = new byte[dataOffset + entries.Sum(entry => entry.Data.Length)];

        BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)tableEntryCount);
        var currentOffset = dataOffset;
        for (var index = 0; index < entries.Length; index++)
        {
            WriteTableEntry(bytes, index, currentOffset, entries[index].Name);
            entries[index].Data.CopyTo(bytes, currentOffset);
            currentOffset += entries[index].Data.Length;
        }

        WriteTableEntry(bytes, entries.Length, currentOffset, string.Empty);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    public static string WriteInvalidRange(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        var bytes = new byte[HeaderSize + 2 * TableEntrySize];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, 2);
        WriteTableEntry(bytes, 0, bytes.Length + 1, "bad.bin");
        WriteTableEntry(bytes, 1, bytes.Length, string.Empty);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static void WriteTableEntry(byte[] destination, int index, int offset, string name)
    {
        var baseOffset = HeaderSize + index * TableEntrySize;
        BinaryPrimitives.WriteUInt32LittleEndian(destination.AsSpan(baseOffset), (uint)offset);
        var nameBytes = Encoding.ASCII.GetBytes(name);
        nameBytes.AsSpan(0, Math.Min(nameBytes.Length, 13)).CopyTo(destination.AsSpan(baseOffset + 4, 13));
    }
}
