using System;
using System.IO;
using System.Reflection;
using Arbiter.App.Services.Client;

namespace Arbiter.App.Tests.Services.Client;

[TestFixture]
public class GameClientServiceTests
{
    private static readonly MethodInfo ApplySuppressLoginNoticePatch = typeof(GameClientService).GetMethod(
        "ApplySuppressLoginNoticePatch", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Test]
    public void Should_Replace_Expected_Login_Notification_Patches()
    {
        var memory = new byte[0x56485A];
        memory[0x4B897C] = 0x75;
        memory[0x4B897D] = 0x6C;
        memory[0x4B8ACF] = 0x75;
        memory[0x4B8AD0] = 0x6D;
        memory[0x564855] = 0x68;
        memory[0x564856] = 0xE8;
        memory[0x564857] = 0x03;
        memory[0x564858] = 0x00;
        memory[0x564859] = 0x00;

        ApplyPatch(memory);

        Assert.Multiple(() =>
        {
            Assert.That(memory[0x4B897C], Is.EqualTo(0xEB));
            Assert.That(memory[0x4B897D], Is.EqualTo(0x6C));
            Assert.That(memory[0x4B8ACF], Is.EqualTo(0xEB));
            Assert.That(memory[0x4B8AD0], Is.EqualTo(0x6D));
            Assert.That(memory[0x564855], Is.EqualTo(0x68));
            Assert.That(memory[0x564856], Is.EqualTo(0x00));
            Assert.That(memory[0x564857], Is.EqualTo(0x00));
            Assert.That(memory[0x564858], Is.EqualTo(0x00));
            Assert.That(memory[0x564859], Is.EqualTo(0x00));
        });
    }

    [Test]
    public void Should_Reject_Unexpected_Login_Notification_Bytes()
    {
        var memory = new byte[0x56485A];
        memory[0x4B897C] = 0x75;
        memory[0x4B897D] = 0x6C;
        memory[0x4B8ACF] = 0x75;
        memory[0x4B8AD0] = 0x6D;

        var exception = Assert.Throws<TargetInvocationException>(() => ApplyPatch(memory));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(memory[0x4B897C], Is.EqualTo(0x75));
            Assert.That(memory[0x4B8ACF], Is.EqualTo(0x75));
        });
    }

    private static void ApplyPatch(byte[] memory)
    {
        using var stream = new MemoryStream(memory, writable: true);
        using var writer = new BinaryWriter(stream);

        ApplySuppressLoginNoticePatch.Invoke(null, [writer]);
    }
}
