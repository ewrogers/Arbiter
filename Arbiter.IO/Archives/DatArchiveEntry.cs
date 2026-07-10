namespace Arbiter.IO.Archives;

public sealed record DatArchiveEntry(string Name, long Offset, long Length);
