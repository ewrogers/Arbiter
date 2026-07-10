using System.Buffers.Binary;
using Arbiter.Imaging.Formats;

namespace Arbiter.Imaging.Tests.Formats;

public sealed class MpfFileTests
{
    [Test]
    public void Should_Parse_Frames_Animation_Metadata_And_Palette_Record()
    {
        var bytes = TestImageData.Mpf(
            8,
            10,
            [
                new TestMpfFrame(1, 2, 2, 1, 4, 9, [1, 2]),
                new TestMpfFrame(2, 3, 1, 2, 4, 9, [3, 4])
            ],
            paletteNumber: 17,
            walkFrameIndex: 0,
            walkFrameCount: 1,
            standingFrameIndex: 1,
            standingFrameCount: 1,
            optionalAnimationFrameCount: 1,
            optionalAnimationRatio: 4,
            attackFrameIndex: 0,
            attackFrameCount: 1);

        var mpf = MpfFile.Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(mpf.HeaderType, Is.EqualTo(MpfHeaderType.None));
            Assert.That(mpf.FormatType, Is.EqualTo(MpfFormatType.SingleAttack));
            Assert.That(mpf.IdleType, Is.EqualTo(MpfIdleType.NormalIdle));
            Assert.That(mpf.AnimationIntervalMs, Is.EqualTo(400));
            Assert.That(mpf.PixelWidth, Is.EqualTo(8));
            Assert.That(mpf.PixelHeight, Is.EqualTo(10));
            Assert.That(mpf.PaletteNumber, Is.EqualTo(17));
            Assert.That(mpf.Frames, Has.Count.EqualTo(2));
            Assert.That(mpf.Frames[0]!.Left, Is.EqualTo(1));
            Assert.That(mpf.Frames[0]!.Top, Is.EqualTo(2));
            Assert.That(mpf.Frames[0]!.CenterX, Is.EqualTo(4));
            Assert.That(mpf.Frames[0]!.CenterY, Is.EqualTo(9));
            Assert.That(mpf.Frames[0]!.Pixels.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(mpf.Frames[1]!.Pixels.ToArray(), Is.EqualTo(new byte[] { 3, 4 }));
        });
    }

    [Test]
    public void Should_Parse_Variable_Unknown_Header_And_Multiple_Attacks()
    {
        var bytes = TestImageData.Mpf(
            1,
            1,
            [new TestMpfFrame(0, 0, 1, 1, 0, 1, [5])],
            standingFrameCount: 2,
            optionalAnimationFrameCount: 1,
            optionalAnimationRatio: 75,
            attack2FrameIndex: 6,
            attack2FrameCount: 2,
            attack3FrameIndex: 8,
            attack3FrameCount: 3,
            multipleAttacks: true,
            includePaletteRecord: false,
            unknownHeader: true,
            unknownHeaderFlags: 0x14,
            unknownHeaderWordCount: 3);

        var mpf = MpfFile.Parse(bytes);

        Assert.Multiple(() =>
        {
            Assert.That(mpf.HeaderType, Is.EqualTo(MpfHeaderType.Unknown));
            Assert.That(mpf.UnknownHeaderBytes.Length, Is.EqualTo(20));
            Assert.That(mpf.FormatType, Is.EqualTo(MpfFormatType.MultipleAttacks));
            Assert.That(mpf.IdleType, Is.EqualTo(MpfIdleType.NormalPlusOptional));
            Assert.That(mpf.OptionalAnimationProbability, Is.EqualTo(75));
            Assert.That(mpf.Attack2FrameIndex, Is.EqualTo(6));
            Assert.That(mpf.Attack2FrameCount, Is.EqualTo(2));
            Assert.That(mpf.Attack3FrameIndex, Is.EqualTo(8));
            Assert.That(mpf.Attack3FrameCount, Is.EqualTo(3));
            Assert.That(mpf.PaletteNumber, Is.Zero);
            Assert.That(mpf.Frames, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Should_Reject_Frame_Data_Outside_The_File()
    {
        var bytes = TestImageData.Mpf(
            1,
            1,
            [new TestMpfFrame(0, 0, 1, 1, 0, 1, [5])],
            includePaletteRecord: false);
        var frameStartOffset = bytes.Length - 1 - 16;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(frameStartOffset + 12), int.MaxValue);

        Assert.That(() => MpfFile.Parse(bytes), Throws.TypeOf<InvalidDataException>());
    }
}
