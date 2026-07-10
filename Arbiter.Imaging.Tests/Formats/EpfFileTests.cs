using System.Buffers.Binary;
using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Tests.Formats;

public sealed class EpfFileTests
{
    [Test]
    public void Should_Parse_Frames_And_Empty_Frame_Entries()
    {
        var epf = EpfFile.Parse(TestImageData.Epf(2, 1, [1, 2], null));

        Assert.Multiple(() =>
        {
            Assert.That(epf.PixelWidth, Is.EqualTo(2));
            Assert.That(epf.PixelHeight, Is.EqualTo(1));
            Assert.That(epf.Frames, Has.Count.EqualTo(2));
            Assert.That(epf.Frames[0]!.Pixels.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(epf.Frames[1], Is.Null);
        });
    }

    [Test]
    public void Should_Reject_Frame_Data_Outside_The_File()
    {
        var bytes = TestImageData.Epf(1, 1, [1]);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(bytes.Length - 8), uint.MaxValue);

        Assert.That(() => EpfFile.Parse(bytes), Throws.TypeOf<OverflowException>().Or.TypeOf<InvalidDataException>());
    }
}
