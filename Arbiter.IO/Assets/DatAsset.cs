using Arbiter.IO.Archives;

namespace Arbiter.IO.Assets;

public sealed class DatAsset
{
    private readonly DatArchive _archive;
    private readonly DatArchiveEntry _entry;

    public string Name => _entry.Name;
    public string ArchiveName => _archive.Name;
    public long Length => _entry.Length;

    internal DatAsset(DatArchive archive, DatArchiveEntry entry)
    {
        _archive = archive;
        _entry = entry;
    }

    public Stream OpenRead() => _archive.OpenRead(_entry);
}
