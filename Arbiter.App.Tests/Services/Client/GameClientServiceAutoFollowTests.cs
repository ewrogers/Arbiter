using System;
using System.Reflection;
using Arbiter.App.Services.Client;

namespace Arbiter.App.Tests.Services.Client;

[TestFixture]
public sealed class GameClientServiceAutoFollowTests
{
    private static readonly MethodInfo BuildImprovedAutoFollowState = GetMethod("BuildImprovedAutoFollowState");
    private static readonly MethodInfo BuildImprovedAutoFollowStub = GetMethod("BuildImprovedAutoFollowStub");
    private static readonly MethodInfo BuildImprovedAutoFollowHook = GetMethod("BuildImprovedAutoFollowHook");

    [Test]
    public void Should_Build_Improved_Auto_Follow_State_With_Three_Tile_Non_Attacking_Defaults()
    {
        var state = (byte[])BuildImprovedAutoFollowState.Invoke(null, null)!;

        Assert.Multiple(() =>
        {
            Assert.That(state, Has.Length.EqualTo(0x18));
            Assert.That(BitConverter.ToInt32(state, 0x0C), Is.EqualTo(3));
            Assert.That(BitConverter.ToInt32(state, 0x10), Is.EqualTo(3));
            Assert.That(state[0x14], Is.Zero);
            Assert.That(state[0x15], Is.Zero);
            Assert.That(state[0x16], Is.Zero);
        });
    }

    [Test]
    public void Should_Build_Improved_Auto_Follow_Stub_With_Resolved_State_Addresses()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;
        var stateAddress = (IntPtr)0x00B70000;

        var stub = (byte[])BuildImprovedAutoFollowStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub, Has.Length.EqualTo(0x1D5));
            Assert.That(stub[0x000..0x009],
                Is.EqualTo(new byte[] { 0x8B, 0x45, 0x0C, 0xF6, 0x40, 0x2C, 0x04, 0x74, 0x2F }));

            AssertAbsoluteAddresses(stub, 0x00B70000, 0x00B, 0x04F, 0x080, 0x0B6, 0x126);
            AssertAbsoluteAddresses(stub, 0x00B70004, 0x014, 0x08B, 0x0D7, 0x12E);
            AssertAbsoluteAddresses(stub, 0x00B70008, 0x05D, 0x09B, 0x0C4, 0x0CC);
            AssertAbsoluteAddresses(stub, 0x00B7000C, 0x01E, 0x0EE, 0x14E);
            AssertAbsoluteAddresses(stub, 0x00B70010, 0x019);
            AssertAbsoluteAddresses(stub, 0x00B70014, 0x02E, 0x03A, 0x046, 0x074, 0x0AA, 0x161, 0x179);
            AssertAbsoluteAddresses(stub, 0x00B70015, 0x028, 0x065, 0x15B);
            AssertAbsoluteAddresses(stub, 0x00B70016, 0x023);
        });
    }

    [Test]
    public void Should_Build_Improved_Auto_Follow_Stub_With_Resolved_Client_Targets()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;
        var stateAddress = (IntPtr)0x00B70000;

        var stub = (byte[])BuildImprovedAutoFollowStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress])!;

        Assert.Multiple(() =>
        {
            AssertRelativeTargets(stub, stubAddress, 0x005F4A70, 0x034, 0x040, 0x16D);
            AssertRelativeTargets(stub, stubAddress, 0x005F44B0, 0x06E);
            AssertRelativeTargets(stub, stubAddress, stubAddress.ToInt64() + 0x190, 0x0A4);
            AssertRelativeTargets(stub, stubAddress, 0x005F4AAD, 0x192);
            AssertRelativeTargets(stub, stubAddress, 0x005F4AA8, 0x197);
            AssertRelativeTargets(stub, stubAddress, 0x005F4900, 0x106, 0x185);
            AssertRelativeTargets(stub, stubAddress, 0x005F4A5B, 0x10B);
            AssertRelativeTargets(stub, stubAddress, 0x005F49EF, 0x11A);
            AssertRelativeTargets(stub, stubAddress, 0x005D48F1, 0x1A1, 0x1C2, 0x1CC);
            AssertRelativeTargets(stub, stubAddress, 0x005D48E5, 0x1AB, 0x1B8, 0x1D1);
        });
    }

    [Test]
    public void Should_Replay_The_Generation_Branch_Outside_The_Overwritten_Hook()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;
        var stateAddress = (IntPtr)0x00B70000;

        var stub = (byte[])BuildImprovedAutoFollowStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub[0x09F..0x0A4], Is.EqualTo(new byte[] { 0x83, 0x7D, 0x08, 0x00, 0xE9 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x0A4),
                Is.EqualTo(stubAddress.ToInt64() + 0x190));

            Assert.That(stub[0x190..0x192], Is.EqualTo(new byte[] { 0x0F, 0x85 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x192), Is.EqualTo(0x005F4AAD));
            Assert.That(stub[0x196], Is.EqualTo(0xE9));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x197), Is.EqualTo(0x005F4AA8));
        });
    }

    [TestCase(0x005EF33A, 0x000, 0xE8, 5)]
    [TestCase(0x005D48DF, 0x19B, 0xE9, 6)]
    [TestCase(0x005F49E5, 0x0A8, 0xE9, 10)]
    [TestCase(0x005F4AA2, 0x072, 0xE9, 6)]
    [TestCase(0x005F4D0E, 0x044, 0xE8, 5)]
    public void Should_Build_Improved_Auto_Follow_Hooks(int hookAddressValue, int stubOffset,
        byte opcode, int hookLength)
    {
        var hookAddress = (IntPtr)hookAddressValue;
        var stubAddress = (IntPtr)0x10000000;

        var hook = (byte[])BuildImprovedAutoFollowHook.Invoke(null,
            [hookAddress, stubAddress, stubOffset, opcode, hookLength])!;

        Assert.Multiple(() =>
        {
            Assert.That(hook, Has.Length.EqualTo(hookLength));
            Assert.That(hook[0], Is.EqualTo(opcode));
            Assert.That(GetRelativeTarget(hook, hookAddress, 1), Is.EqualTo(stubAddress.ToInt64() + stubOffset));
            Assert.That(hook[5..], Is.All.EqualTo(0x90));
        });
    }

    [Test]
    public void Should_Allow_Only_Shift_Pursuit_Dispatch_For_Living_Objects()
    {
        var moduleBaseAddress = (IntPtr)0x00400000;
        var stubAddress = (IntPtr)0x10000000;
        var stateAddress = (IntPtr)0x00B70000;

        var stub = (byte[])BuildImprovedAutoFollowStub.Invoke(null,
            [moduleBaseAddress, stubAddress, stateAddress])!;

        Assert.Multiple(() =>
        {
            Assert.That(stub[0x19B..0x19F],
                Is.EqualTo(new byte[] { 0x83, 0x7D, 0xA0, 0x08 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x1A1), Is.EqualTo(0x005D48F1));

            Assert.That(stub[0x1A5..0x1A9],
                Is.EqualTo(new byte[] { 0x80, 0x7D, 0xC3, 0x00 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x1AB), Is.EqualTo(0x005D48E5));

            Assert.That(stub[0x1AF..0x1B6],
                Is.EqualTo(new byte[] { 0x8B, 0x45, 0x0C, 0x80, 0x78, 0x0C, 0x05 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x1B8), Is.EqualTo(0x005D48E5));

            Assert.That(stub[0x1BC..0x1C0],
                Is.EqualTo(new byte[] { 0x83, 0x7D, 0xA0, 0x01 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x1C2), Is.EqualTo(0x005D48F1));
            Assert.That(stub[0x1C6..0x1CA],
                Is.EqualTo(new byte[] { 0x83, 0x7D, 0xA0, 0x02 }));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x1CC), Is.EqualTo(0x005D48F1));
            Assert.That(stub[0x1D0], Is.EqualTo(0xE9));
            Assert.That(GetRelativeTarget(stub, stubAddress, 0x1D1), Is.EqualTo(0x005D48E5));
        });
    }

    private static void AssertAbsoluteAddresses(byte[] stub, uint expected, params int[] offsets)
    {
        foreach (var offset in offsets)
        {
            Assert.That(BitConverter.ToUInt32(stub, offset), Is.EqualTo(expected),
                $"Absolute address at stub + 0x{offset:X3}");
        }
    }

    private static void AssertRelativeTargets(byte[] stub, IntPtr stubAddress, long expected, params int[] offsets)
    {
        foreach (var offset in offsets)
        {
            Assert.That(GetRelativeTarget(stub, stubAddress, offset), Is.EqualTo(expected),
                $"Relative target at stub + 0x{offset:X3}");
        }
    }

    private static long GetRelativeTarget(byte[] code, IntPtr codeAddress, int operandOffset)
    {
        var relativeOffset = BitConverter.ToInt32(code, operandOffset);
        return codeAddress.ToInt64() + operandOffset + sizeof(int) + relativeOffset;
    }

    private static MethodInfo GetMethod(string name) => typeof(GameClientService).GetMethod(name,
        BindingFlags.NonPublic | BindingFlags.Static)!;
}
