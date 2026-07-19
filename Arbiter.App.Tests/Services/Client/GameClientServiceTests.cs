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
    private static readonly MethodInfo BuildGroundItemCollectorStub = typeof(GameClientService).GetMethod(
        "BuildGroundItemCollectorStub", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildGroundItemFrameStub = typeof(GameClientService).GetMethod(
        "BuildGroundItemFrameStub", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildGroundItemKeyTransitionStub = typeof(GameClientService).GetMethod(
        "BuildGroundItemKeyTransitionStub", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildGroundItemHook = typeof(GameClientService).GetMethod(
        "BuildGroundItemHook", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo GroundItemKeyDownStubTemplate = typeof(GameClientService).GetField(
        "GroundItemKeyDownStubTemplate", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo GroundItemKeyUpStubTemplate = typeof(GameClientService).GetField(
        "GroundItemKeyUpStubTemplate", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly FieldInfo ExpectedStaticRenderModeSelector = typeof(GameClientService).GetField(
        "ExpectedStaticRenderModeSelector", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildSkipExchangeQuantityPromptStub = typeof(GameClientService).GetMethod(
        "BuildSkipExchangeQuantityPromptStub", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildSkipExchangeQuantityPromptHook = typeof(GameClientService).GetMethod(
        "BuildSkipExchangeQuantityPromptHook", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildShowItemQuantityInDialogsStub = typeof(GameClientService).GetMethod(
        "BuildShowItemQuantityInDialogsStub", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo BuildShowItemQuantityInDialogsHook = typeof(GameClientService).GetMethod(
        "BuildShowItemQuantityInDialogsHook", BindingFlags.NonPublic | BindingFlags.Static)!;

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

    [Test]
    public void Should_Build_255_Item_Collector_Stub_With_Resolved_Addresses()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;
        var stateAddress = (IntPtr)0x00B70000;

        var stub = (byte[])BuildGroundItemCollectorStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(157));
            Assert.That(stub[0..11], Is.EqualTo(new byte[]
            {
                0x55, 0x89, 0xE5, 0x83, 0xEC, 0x08, 0x53, 0x56, 0x57, 0x89, 0xCE,
            }));
            Assert.That(BitConverter.ToUInt32(stub, 0x3A), Is.EqualTo(0x0068B1AC));
            Assert.That(BitConverter.ToUInt32(stub, 0x4A), Is.EqualTo(0x00B70000));
            Assert.That(stub[0x4E..0x53], Is.EqualTo(new byte[] { 0x3D, 0xFF, 0x00, 0x00, 0x00 }));
            Assert.That(BitConverter.ToUInt32(stub, 0x5A), Is.EqualTo(0x00B70100));
            Assert.That(BitConverter.ToUInt32(stub, 0x70), Is.EqualTo(0x00B70000));
            Assert.That(BitConverter.ToUInt32(stub, 0x76), Is.EqualTo(0x00B70004));
            Assert.That(BitConverter.ToUInt32(stub, 0x7E), Is.EqualTo(0x00B70008));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x98), Is.EqualTo(0x005D3745));
        });
    }

    [Test]
    public void Should_Build_Final_Frame_Replay_Stub_With_Resolved_Addresses()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x20000000;
        var stateAddress = (IntPtr)0x00B70000;

        var stub = (byte[])BuildGroundItemFrameStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(186));
            Assert.That(BitConverter.ToUInt32(stub, 0x0D), Is.EqualTo(0x00B70028));
            Assert.That(BitConverter.ToUInt32(stub, 0x13), Is.EqualTo(0x00B70000));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x25), Is.EqualTo(0x00427380));
            Assert.That(stub[0x2E..0x35],
                Is.EqualTo(new byte[] { 0xF6, 0x80, 0x34, 0x04, 0x00, 0x00, 0x01 }));
            Assert.That(BitConverter.ToUInt32(stub, 0x3B), Is.EqualTo(0x00B70000));
            Assert.That(BitConverter.ToUInt32(stub, 0x46), Is.EqualTo(0x00B70100));
            Assert.That(BitConverter.ToUInt32(stub, 0x52), Is.EqualTo(0x0068B1AC));
            Assert.That(BitConverter.ToUInt32(stub, 0x79), Is.EqualTo(0x00B70004));
            Assert.That(BitConverter.ToUInt32(stub, 0x8D), Is.EqualTo(0x00B70008));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x91), Is.EqualTo(0x005D3190));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0xB5), Is.EqualTo(0x005CE286));
        });
    }

    [TestCase(false, 0x00067C10, 0x00467C15)]
    [TestCase(true, 0x00067E30, 0x00467E35)]
    public void Should_Build_Raw_Alt_Key_Invalidation_Stubs_With_Resolved_Addresses(bool keyUp, int hookRva,
        int expectedContinuation)
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x20000000;
        var stateAddress = (IntPtr)0x00B70000;
        var templateField = keyUp ? GroundItemKeyUpStubTemplate : GroundItemKeyDownStubTemplate;
        var template = (byte[])templateField.GetValue(null)!;

        var stub = (byte[])BuildGroundItemKeyTransitionStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress, hookRva, template])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(84));
            Assert.That(stub[0x1F..0x2C], Is.EqualTo(new byte[]
            {
                0x0F, 0xB6, 0x45, 0x08, 0x83, 0xF8, 0x38, 0x74, 0x07, 0x3D, 0xB8, 0x00, 0x00,
            }));
            Assert.That(BitConverter.ToUInt32(stub, 0x31), Is.EqualTo(0x00B70028));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x3B), Is.EqualTo(0x00549F60));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x4F), Is.EqualTo(expectedContinuation));
        });
    }

    [Test]
    public void Should_Leave_The_Static_Render_Mode_Selector_Unchanged()
    {
        var expected = (byte[])ExpectedStaticRenderModeSelector.GetValue(null)!;

        Assert.That(expected, Is.EqualTo(new byte[]
        {
            0x8B, 0x55, 0xD0, 0x0F, 0xB6, 0x82, 0xB9, 0x00, 0x00, 0x00, 0x25, 0x80, 0x00, 0x00, 0x00, 0x74,
            0x09, 0xC7, 0x45, 0xE8, 0x6D, 0x00, 0x00, 0x00, 0xEB, 0x16, 0x8B, 0x4D, 0xD0, 0x0F, 0xB6, 0x91,
            0xB9, 0x00, 0x00, 0x00, 0x83, 0xE2, 0x40, 0x74, 0x07, 0xC7, 0x45, 0xE8, 0x03, 0x00, 0x00, 0x00,
        }));
    }

    [Test]
    public void Should_Build_Padded_Jumps_To_The_Ground_Item_Wrappers()
    {
        var hookAddress = (IntPtr)0x005D3740;
        var stubAddress = (IntPtr)0x10000000;

        var fiveByteHook = (byte[])BuildGroundItemHook.Invoke(null, [hookAddress, stubAddress, 5])!;
        var sixByteHook = (byte[])BuildGroundItemHook.Invoke(null, [hookAddress, stubAddress, 6])!;

        Assert.Multiple(() =>
        {
            Assert.That(fiveByteHook, Has.Length.EqualTo(5));
            Assert.That(fiveByteHook[0], Is.EqualTo(0xE9));
            Assert.That(GetRelativeTarget(fiveByteHook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
            Assert.That(sixByteHook, Has.Length.EqualTo(6));
            Assert.That(sixByteHook[5], Is.EqualTo(0x90));
            Assert.That(GetRelativeTarget(sixByteHook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
        });
    }

    [Test]
    public void Should_Build_Skip_Exchange_Quantity_Prompt_Stub_With_Resolved_Addresses()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x0066867A;

        var stub = (byte[])BuildSkipExchangeQuantityPromptStub.Invoke(null,
            [moduleBaseAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(125));
            Assert.That(stub[0..16], Is.EqualTo(new byte[]
            {
                0x53, 0x56, 0x57, 0x89, 0xCE, 0x8B, 0x7C, 0x24,
                0x10, 0x0F, 0xB6, 0x5F, 0x02, 0x84, 0xDB, 0x74,
            }));
            Assert.That(stub[0x17..0x1B], Is.EqualTo(new byte[] { 0xAB, 0x15, 0xF4, 0xFF }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x16), Is.EqualTo(0x005A9C40));
            Assert.That(stub[0x5D..0x61], Is.EqualTo(new byte[] { 0xC5, 0x3B, 0xE0, 0xFF }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x5C), Is.EqualTo(0x0046C2A0));
            Assert.That(stub[0x79..0x7D], Is.EqualTo(new byte[] { 0x9E, 0x1F, 0xE0, 0xFF }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x78), Is.EqualTo(0x0046A695));
        });
    }

    [Test]
    public void Should_Build_Only_A_Five_Byte_Jump_To_The_Exchange_Quantity_Prompt_Stub()
    {
        var hookAddress = (IntPtr)0x0046A690;
        var stubAddress = (IntPtr)0x0066867A;

        var hook = (byte[])BuildSkipExchangeQuantityPromptHook.Invoke(null, [hookAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(hook, Is.EqualTo(new byte[] { 0xE9, 0xE5, 0xDF, 0x1F, 0x00 }));
            Assert.That(GetRelativeTarget(hook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
        });
    }

    [Test]
    public void Should_Build_Show_Item_Quantity_In_Dialogs_Stub_With_Resolved_Addresses()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;

        var stub = (byte[])BuildShowItemQuantityInDialogsStub.Invoke(null,
            [moduleBaseAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(341));
            Assert.That(stub[0x0C..0x23], Is.EqualTo(new byte[]
            {
                0x8B, 0x75, 0x10, 0x89, 0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0x8B, 0x7D, 0x00,
                0x8B, 0x7F, 0xE8, 0x0F, 0xB6, 0x3F, 0x90, 0x90, 0x90, 0x90, 0x90,
            }));
            Assert.That(GetNearConditionalTarget(stub, 0x26), Is.EqualTo(0x139));
            Assert.That(GetNearConditionalTarget(stub, 0x2F), Is.EqualTo(0x139));
            Assert.That(GetNearConditionalTarget(stub, 0x3C), Is.EqualTo(0x139));
            Assert.That(stub[0x72..0x7A],
                Is.EqualTo(new byte[] { 0x49, 0x0F, 0x8E, 0xC0, 0x00, 0x00, 0x00, 0x41 }));
            Assert.That(GetNearConditionalTarget(stub, 0x73), Is.EqualTo(0x139));
            Assert.That(stub[0x36..0x3A], Is.EqualTo(new byte[] { 0x06, 0x9C, 0x5A, 0xF0 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x35), Is.EqualTo(0x005A9C40));
            Assert.That(stub[0x99..0x9D], Is.EqualTo(new byte[] { 0x80, 0x93, 0x66, 0x00 }));
            Assert.That(BitConverter.ToUInt32(stub, 0x99), Is.EqualTo(0x00669380));
            Assert.That(BitConverter.ToInt32(stub, 0xCC), Is.EqualTo(20));
            Assert.That(stub[0xDA..0xDD], Is.EqualTo(new byte[] { 0x83, 0xEA, 0x02 }));
            Assert.That(stub[0x109..0x115], Is.EqualTo(new byte[]
            {
                0x66, 0xC7, 0x07, 0x2E, 0x2E, 0x90, 0x90, 0x90, 0x90, 0x83, 0xC7, 0x02,
            }));
            Assert.That(stub[0xF4..0xF8], Is.EqualTo(new byte[] { 0x78, 0xD5, 0x47, 0xF0 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0xF3), Is.EqualTo(0x0047D670));
            Assert.That(stub[0x133..0x139], Is.EqualTo(new byte[] { 0x89, 0x85, 0xD8, 0xFE, 0xFF, 0xFF }));
            Assert.That(stub[0x139..0x145], Is.EqualTo(new byte[]
            {
                0xFF, 0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0xFF, 0x75, 0x0C, 0xFF, 0x75, 0x08,
            }));
            Assert.That(stub[0x146..0x14A], Is.EqualTo(new byte[] { 0x7A, 0x27, 0x62, 0xF0 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x145), Is.EqualTo(0x006228C4));
            Assert.That(stub[^8..], Is.EqualTo(new byte[] { 0x8D, 0x65, 0xF4, 0x5F, 0x5E, 0x5B, 0x5D, 0xC3 }));
        });
    }

    [Test]
    public void Should_Build_Only_A_Five_Byte_Call_To_The_Dialog_Item_Quantity_Stub()
    {
        var hookAddress = (IntPtr)0x0053609C;
        var stubAddress = (IntPtr)0x10000000;

        var hook = (byte[])BuildShowItemQuantityInDialogsHook.Invoke(null, [hookAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(hook, Is.EqualTo(new byte[] { 0xE8, 0x5F, 0x9F, 0xAC, 0x0F }));
            Assert.That(GetRelativeTarget(hook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
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

    private static int GetNearConditionalTarget(byte[] code, int instructionOffset) =>
        instructionOffset + 6 + BitConverter.ToInt32(code, instructionOffset + 2);
}
