using Arbiter.IO.Archives;

namespace Arbiter.IO.Tests.Archives;

public sealed class DatArchiveTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "Arbiter.IO.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_directory, true);
    }

    [Test]
    public void Should_Read_Entries_Through_Bounded_Streams()
    {
        var path = MockDatArchive.Write(
            _directory,
            "mock.dat",
            ("first.bin", [1, 2, 3]),
            ("second.bin", [4, 5, 6, 7]));

        var archive = DatArchive.Open(path);
        using var stream = archive.OpenRead(archive.Entries[0]);
        var bytes = new byte[10];
        var bytesRead = stream.Read(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(archive.Entries.Select(entry => entry.Name), Is.EqualTo(new[] { "first.bin", "second.bin" }));
            Assert.That(stream.Length, Is.EqualTo(3));
            Assert.That(bytesRead, Is.EqualTo(3));
            Assert.That(bytes[..bytesRead], Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(stream.ReadByte(), Is.EqualTo(-1));
        });
    }

    [Test]
    public void Should_Seek_Only_Inside_An_Entry()
    {
        var path = MockDatArchive.Write(_directory, "mock.dat", ("value.bin", [10, 20, 30]));
        var archive = DatArchive.Open(path);
        using var stream = archive.OpenRead(archive.Entries[0]);

        stream.Seek(-1, SeekOrigin.End);

        Assert.Multiple(() =>
        {
            Assert.That(stream.ReadByte(), Is.EqualTo(30));
            Assert.That(() => stream.Seek(1, SeekOrigin.End), Throws.TypeOf<IOException>());
        });
    }

    [Test]
    public void Should_Reject_Archive_Entries_Outside_File_Bounds()
    {
        var path = MockDatArchive.WriteInvalidRange(_directory, "invalid.dat");

        Assert.That(() => DatArchive.Open(path), Throws.TypeOf<InvalidDataException>());
    }
}
