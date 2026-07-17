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
    public void Should_Replace_Expected_Login_Notification_Jumps()
    {
        var memory = new byte[0x4B8AD1];
        memory[0x4B897C] = 0x75;
        memory[0x4B897D] = 0x6C;
        memory[0x4B8ACF] = 0x75;
        memory[0x4B8AD0] = 0x6D;

        ApplyPatch(memory);

        Assert.Multiple(() =>
        {
            Assert.That(memory[0x4B897C], Is.EqualTo(0xEB));
            Assert.That(memory[0x4B897D], Is.EqualTo(0x6C));
            Assert.That(memory[0x4B8ACF], Is.EqualTo(0xEB));
            Assert.That(memory[0x4B8AD0], Is.EqualTo(0x6D));
        });
    }

    [Test]
    public void Should_Reject_Unexpected_Login_Notification_Bytes()
    {
        var memory = new byte[0x4B8AD1];

        var exception = Assert.Throws<TargetInvocationException>(() => ApplyPatch(memory));

        Assert.That(exception!.InnerException, Is.TypeOf<InvalidDataException>());
    }

    private static void ApplyPatch(byte[] memory)
    {
        using var stream = new MemoryStream(memory, writable: true);
        using var writer = new BinaryWriter(stream);

        ApplySuppressLoginNoticePatch.Invoke(null, [writer]);
    }
}
