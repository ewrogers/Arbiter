using System.Reflection;
using Arbiter.App.Services.Client;

namespace Arbiter.App.Tests.Services.Client;

[TestFixture]
public sealed class GameClientServiceExchangeUiTests
{
    private static readonly MethodInfo BuildExchangeResultHandlerStub = GetMethod(
        "BuildExchangeResultHandlerStub");
    private static readonly MethodInfo BuildExchangeEntryHook = GetMethod("BuildExchangeEntryHook");
    private static readonly FieldInfo ExchangeDialogDraggableReplacement = GetField(
        "ExchangeDialogDraggableReplacement");
    private static readonly FieldInfo SuppressExchangeAlertReplacement = GetField(
        "SuppressExchangeAlertReplacement");

    [Test]
    public void Should_Build_Draggable_And_No_Alert_Replacements()
    {
        var draggable = (byte[])ExchangeDialogDraggableReplacement.GetValue(null)!;
        var noAlert = (byte[])SuppressExchangeAlertReplacement.GetValue(null)!;

        Assert.Multiple(() =>
        {
            Assert.That(draggable,
                Is.EqualTo(new byte[] { 0xC7, 0x82, 0x2C, 0x06, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 }));
            Assert.That(noAlert,
                Is.EqualTo(new byte[] { 0x31, 0xC0, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }));
        });
    }

    [TestCase(false, 0x0046A9E5)]
    [TestCase(true, 0x0046AB25)]
    public void Should_Build_Bounded_Exchange_Result_Stubs(bool accepted, long expectedContinuation)
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;

        var stub = (byte[])BuildExchangeResultHandlerStub.Invoke(null,
            [moduleBaseAddress, stubAddress, accepted])!;
        var callTargets = GetRelativeCallTargets(stub, stubAddress);

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.LessThanOrEqualTo(256));
            Assert.That(stub[0..12], Is.EqualTo(new byte[]
            {
                0x55, 0x89, 0xE5, 0x81, 0xEC, 0x88, 0x00, 0x00, 0x00, 0x53, 0x56, 0x57,
            }));
            Assert.That(ContainsSequence(stub,
                [0x81, 0xF9, 0x82, 0x00, 0x00, 0x00, 0x76, 0x05, 0xB9, 0x82, 0x00, 0x00, 0x00]),
                Is.True);
            Assert.That(ContainsSequence(stub, [0x68, 0x68, 0xBC, 0x68, 0x00]), Is.True);
            Assert.That(callTargets.Count(x => x == 0x004803A0), Is.EqualTo(2));
            Assert.That(stub[^10..^5], Is.EqualTo(new byte[] { 0x55, 0x8B, 0xEC, 0x6A, 0xFF }));
            Assert.That(GetRelativeTarget(stub, stubAddress, stub.Length - 5), Is.EqualTo(expectedContinuation));
        });

        if (accepted)
        {
            Assert.Multiple(() =>
            {
                Assert.That(ContainsSequence(stub, [0x80, 0x7B, 0x02, 0x00]), Is.True);
                Assert.That(ContainsSequence(stub, [0x80, 0xB8, 0x36, 0x06, 0x00, 0x00, 0x01]), Is.True);
                Assert.That(ContainsSequence(stub, [0x80, 0xB8, 0x35, 0x06, 0x00, 0x00, 0x01]), Is.True);
            });
        }
    }

    [Test]
    public void Should_Build_Only_A_Five_Byte_Jump_To_An_Exchange_Ui_Stub()
    {
        var hookAddress = (IntPtr)0x00469560;
        var stubAddress = (IntPtr)0x10000000;

        var hook = (byte[])BuildExchangeEntryHook.Invoke(null, [hookAddress, stubAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(hook, Has.Length.EqualTo(5));
            Assert.That(hook[0], Is.EqualTo(0xE9));
            Assert.That(GetRelativeTarget(hook, hookAddress, 0), Is.EqualTo(stubAddress.ToInt64()));
        });
    }

    private static MethodInfo GetMethod(string name) => typeof(GameClientService).GetMethod(name,
        BindingFlags.NonPublic | BindingFlags.Static)!;

    private static FieldInfo GetField(string name) => typeof(GameClientService).GetField(name,
        BindingFlags.NonPublic | BindingFlags.Static)!;

    private static List<long> GetRelativeCallTargets(byte[] code, IntPtr codeAddress)
    {
        var targets = new List<long>();
        for (var offset = 0; offset <= code.Length - 5; offset++)
        {
            if (code[offset] == 0xE8)
            {
                targets.Add(GetRelativeTarget(code, codeAddress, offset));
            }
        }

        return targets;
    }

    private static long GetRelativeTarget(byte[] code, IntPtr codeAddress, int instructionOffset)
    {
        var relativeOffset = BitConverter.ToInt32(code, instructionOffset + 1);
        return codeAddress.ToInt64() + instructionOffset + 5 + relativeOffset;
    }

    private static bool ContainsSequence(byte[] code, byte[] sequence) => code.AsSpan().IndexOf(sequence) >= 0;
}
