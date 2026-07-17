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
    private static readonly MethodInfo BuildStuckModifierFixStub = typeof(GameClientService).GetMethod(
        "BuildStuckModifierFixStub", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildStuckModifierCall = typeof(GameClientService).GetMethod(
        "BuildStuckModifierCall", BindingFlags.NonPublic | BindingFlags.Static)!;

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

    [Test]
    public void Should_Build_Stuck_Modifier_Fix_Stub_With_Resolved_Addresses()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x01000000;

        var stub = (byte[])BuildStuckModifierFixStub.Invoke(null, [moduleBaseAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(68));
            Assert.That(stub[0..2], Is.EqualTo(new byte[] { 0x9C, 0x60 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 2), Is.EqualTo(0x00427380));
            Assert.That(stub[7..13], Is.EqualTo(new byte[] { 0x85, 0xC0, 0x74, 0x32, 0x89, 0xC3 }));
            Assert.That(GetShortTarget(stub, 9), Is.EqualTo(61));
            Assert.That(stub[13], Is.EqualTo(0xE8));
            Assert.That(GetRelativeTarget(stub, stubAddress, 13), Is.EqualTo(0x0062006E));
            Assert.That(stub[22..32],
                Is.EqualTo(new byte[] { 0xF6, 0x84, 0x33, 0x34, 0x03, 0x00, 0x00, 0x80, 0x74, 0x0D }));
            Assert.That(GetShortTarget(stub, 30), Is.EqualTo(45));
            Assert.That(stub[32..40],
                Is.EqualTo(new byte[] { 0x6A, 0x00, 0x57, 0x6A, 0x00, 0x56, 0x89, 0xD9 }));
            Assert.That(stub[40], Is.EqualTo(0xE8));
            Assert.That(GetRelativeTarget(stub, stubAddress, 40), Is.EqualTo(0x00466E60));
            Assert.That(stub[45..61], Is.EqualTo(new byte[]
            {
                0x46, 0x81, 0xFE, 0x00, 0x01, 0x00, 0x00, 0x7C, 0xE0,
                0xC6, 0x83, 0x34, 0x04, 0x00, 0x00, 0x00,
            }));
            Assert.That(GetShortTarget(stub, 52), Is.EqualTo(22));
            Assert.That(stub[61..63], Is.EqualTo(new byte[] { 0x61, 0x9D }));
            Assert.That(stub[63], Is.EqualTo(0xE9));
            Assert.That(GetRelativeTarget(stub, stubAddress, 63), Is.EqualTo(0x004AC950));
        });
    }

    [Test]
    public void Should_Build_Only_A_Five_Byte_Call_To_The_Stuck_Modifier_Stub()
    {
        var callAddress = (IntPtr)0x004A9D81;
        var stubAddress = (IntPtr)0x01000000;

        var call = (byte[])BuildStuckModifierCall.Invoke(null, [callAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(call, Has.Length.EqualTo(5));
            Assert.That(call[0], Is.EqualTo(0xE8));
            Assert.That(GetRelativeTarget(call, callAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
        });
    }

    private static void ApplyPatch(byte[] memory)
    {
        using var stream = new MemoryStream(memory, writable: true);
        using var writer = new BinaryWriter(stream);

        ApplySuppressLoginNoticePatch.Invoke(null, [writer]);
    }

    private static long GetRelativeTarget(byte[] code, IntPtr codeAddress, int instructionOffset)
    {
        var relativeOffset = BitConverter.ToInt32(code, instructionOffset + 1);
        return codeAddress.ToInt64() + instructionOffset + 5 + relativeOffset;
    }

    private static int GetShortTarget(byte[] code, int instructionOffset) =>
        instructionOffset + 2 + unchecked((sbyte)code[instructionOffset + 1]);
}
