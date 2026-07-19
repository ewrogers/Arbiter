using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbiter.Interop.Process;

namespace Arbiter.App.Services.Client;

public partial class GameClientService
{
    private const int GroundItemCapacity = 255;
    private const int GroundItemStateEntryOffset = 0x100;
    private const int GroundItemStatePaneOffset = 0x28;
    private const int GroundItemStateSize = GroundItemStateEntryOffset + GroundItemCapacity * 12;

    private const int GroundItemCollectorHookRva = 0x001D3740;
    private const int GroundItemFrameHookRva = 0x001CE280;
    private const int GroundItemKeyDownHookRva = 0x00067C10;
    private const int GroundItemKeyUpHookRva = 0x00067E30;
    private const int StaticRenderModeSelectorRva = 0x001E487D;
    private const int RenderWorldObjectRva = 0x001D3190;
    private const int UiPaneInvalidateRva = 0x00149F60;
    private const int WorldItemVtableRva = 0x0028B1AC;

    private static readonly byte[] ExpectedGroundItemCollectorHook = [0x55, 0x8B, 0xEC, 0x6A, 0xFF];
    private static readonly byte[] ExpectedGroundItemFrameHook = [0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x1C];
    private static readonly byte[] ExpectedGroundItemKeyDownHook = [0x55, 0x8B, 0xEC, 0x6A, 0xFF];
    private static readonly byte[] ExpectedGroundItemKeyUpHook = [0x55, 0x8B, 0xEC, 0x6A, 0xFF];
    private static readonly byte[] ExpectedStaticRenderModeSelector =
    [
        0x8B, 0x55, 0xD0, 0x0F, 0xB6, 0x82, 0xB9, 0x00, 0x00, 0x00, 0x25, 0x80, 0x00, 0x00, 0x00, 0x74,
        0x09, 0xC7, 0x45, 0xE8, 0x6D, 0x00, 0x00, 0x00, 0xEB, 0x16, 0x8B, 0x4D, 0xD0, 0x0F, 0xB6, 0x91,
        0xB9, 0x00, 0x00, 0x00, 0x83, 0xE2, 0x40, 0x74, 0x07, 0xC7, 0x45, 0xE8, 0x03, 0x00, 0x00, 0x00,
    ];

    private static readonly byte[] GroundItemCollectorStubTemplate = Convert.FromHexString(
        "5589E583EC0853565789CEFF7518FF7514FF7510FF750CFF750889F1E8720000008945FC8BBEE00000003BBEE4000000" +
        "73558B1785D2744A813A78563412754283BAB0000000017539A1001011113DFF00000073326BD80C81C3001111118B0F" +
        "890B8B4F04894B048B4F08894B08FF05001011118935041011118B4508A30810111183C70CEBA38B45FC5F5E5B89EC" +
        "5DC214005589E56AFFE963FFFF11");

    private static readonly byte[] GroundItemFrameStubTemplate = Convert.FromHexString(
        "5589E583EC1053565789CE893528101111C705001011110000000089F1E88D0000008945FCE8D6FFFFEF85C07477F680" +
        "3404000001746E31FF3B3D0010111173646BC70C8D98001111118B1385D27452813A78563412754A83BAB00000000175" +
        "418955F48B82B00000008945F8C782B0000000030000008B0D041011118B81BC0200008B55F403422C5053FF35081011" +
        "11E86AFFFFF08B55F48B45F88982B000000047EB948B45FC5F5E5B89EC5DC35589E583EC1CE946FFFFF1");

    private static readonly byte[] GroundItemKeyDownStubTemplate = Convert.FromHexString(
        "5589E583EC085689CEFF7514FF7510FF750CFF750889F1E82E0000008945FC0FB6450883F83874073DB800000075118B" +
        "0D2810111185C974076A00E8C0FFFFE28B45FC5E89EC5DC210005589E56AFFE9ACFFFFE3");

    private static readonly byte[] GroundItemKeyUpStubTemplate = Convert.FromHexString(
        "5589E583EC085689CEFF7514FF7510FF750CFF750889F1E82E0000008945FC0FB6450883F83874073DB800000075118B" +
        "0D2810111185C974076A00E8C0FFFFE18B45FC5E89EC5DC210005589E56AFFE9ACFFFFE3");

    private static void ApplyAllowAltToShowGroundItemsPatch(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr moduleBaseAddress)
    {
        var stage = "verify the ground-item hooks";
        var stateAddress = IntPtr.Zero;
        var collectorStubAddress = IntPtr.Zero;
        var frameStubAddress = IntPtr.Zero;
        var keyDownStubAddress = IntPtr.Zero;
        var keyUpStubAddress = IntPtr.Zero;
        var collectorHookWriteStarted = false;
        var frameHookWriteStarted = false;
        var keyDownHookWriteStarted = false;
        var keyUpHookWriteStarted = false;

        try
        {
            var collectorHookAddress = Add(moduleBaseAddress, GroundItemCollectorHookRva);
            var frameHookAddress = Add(moduleBaseAddress, GroundItemFrameHookRva);
            var keyDownHookAddress = Add(moduleBaseAddress, GroundItemKeyDownHookRva);
            var keyUpHookAddress = Add(moduleBaseAddress, GroundItemKeyUpHookRva);

            VerifyExpectedBytes(writer, collectorHookAddress, ExpectedGroundItemCollectorHook);
            VerifyExpectedBytes(writer, frameHookAddress, ExpectedGroundItemFrameHook);
            VerifyExpectedBytes(writer, keyDownHookAddress, ExpectedGroundItemKeyDownHook);
            VerifyExpectedBytes(writer, keyUpHookAddress, ExpectedGroundItemKeyUpHook);

            stage = "verify the original static render-mode selector";
            VerifyExpectedBytes(writer, Add(moduleBaseAddress, StaticRenderModeSelectorRva),
                ExpectedStaticRenderModeSelector);

            stage = "allocate the ground-item state and stubs";
            stateAddress = allocator.AllocMemory(_ => { }, GroundItemStateSize);
            collectorStubAddress = allocator.AllocMemory(_ => { }, GroundItemCollectorStubTemplate.Length);
            frameStubAddress = allocator.AllocMemory(_ => { }, GroundItemFrameStubTemplate.Length);
            keyDownStubAddress = allocator.AllocMemory(_ => { }, GroundItemKeyDownStubTemplate.Length);
            keyUpStubAddress = allocator.AllocMemory(_ => { }, GroundItemKeyUpStubTemplate.Length);

            var collectorStub = BuildGroundItemCollectorStub(moduleBaseAddress, collectorStubAddress, stateAddress);
            var frameStub = BuildGroundItemFrameStub(moduleBaseAddress, frameStubAddress, stateAddress);
            var keyDownStub = BuildGroundItemKeyTransitionStub(moduleBaseAddress, keyDownStubAddress, stateAddress,
                GroundItemKeyDownHookRva, GroundItemKeyDownStubTemplate);
            var keyUpStub = BuildGroundItemKeyTransitionStub(moduleBaseAddress, keyUpStubAddress, stateAddress,
                GroundItemKeyUpHookRva, GroundItemKeyUpStubTemplate);

            stage = "write the ground-item stubs";
            WriteGroundItemStub(writer, collectorStubAddress, collectorStub);
            WriteGroundItemStub(writer, frameStubAddress, frameStub);
            WriteGroundItemStub(writer, keyDownStubAddress, keyDownStub);
            WriteGroundItemStub(writer, keyUpStubAddress, keyUpStub);

            stage = "protect and verify the ground-item stubs";
            ProtectGroundItemStub(writer, allocator, collectorStubAddress, collectorStub);
            ProtectGroundItemStub(writer, allocator, frameStubAddress, frameStub);
            ProtectGroundItemStub(writer, allocator, keyDownStubAddress, keyDownStub);
            ProtectGroundItemStub(writer, allocator, keyUpStubAddress, keyUpStub);
            VerifyRemoteBytes(writer, stateAddress, new byte[GroundItemStateSize]);

            var collectorHook = BuildGroundItemHook(collectorHookAddress, collectorStubAddress,
                ExpectedGroundItemCollectorHook.Length);
            var frameHook = BuildGroundItemHook(frameHookAddress, frameStubAddress,
                ExpectedGroundItemFrameHook.Length);
            var keyDownHook = BuildGroundItemHook(keyDownHookAddress, keyDownStubAddress,
                ExpectedGroundItemKeyDownHook.Length);
            var keyUpHook = BuildGroundItemHook(keyUpHookAddress, keyUpStubAddress,
                ExpectedGroundItemKeyUpHook.Length);

            stage = "write the ground-item collector hook";
            WriteGroundItemHook(writer, allocator, collectorHookAddress, collectorHook,
                ref collectorHookWriteStarted);

            stage = "write the ground-item frame hook";
            WriteGroundItemHook(writer, allocator, frameHookAddress, frameHook, ref frameHookWriteStarted);

            stage = "write the ground-item key-down hook";
            WriteGroundItemHook(writer, allocator, keyDownHookAddress, keyDownHook, ref keyDownHookWriteStarted);

            stage = "write the ground-item key-up hook";
            WriteGroundItemHook(writer, allocator, keyUpHookAddress, keyUpHook, ref keyUpHookWriteStarted);

            stage = "verify the ground-item hooks";
            VerifyRemoteBytes(writer, collectorHookAddress, collectorHook);
            VerifyRemoteBytes(writer, frameHookAddress, frameHook);
            VerifyRemoteBytes(writer, keyDownHookAddress, keyDownHook);
            VerifyRemoteBytes(writer, keyUpHookAddress, keyUpHook);
        }
        catch (Exception exception)
        {
            var cleanupExceptions = new List<Exception>();
            var collectorHookRestored = !collectorHookWriteStarted;
            var frameHookRestored = !frameHookWriteStarted;
            var keyDownHookRestored = !keyDownHookWriteStarted;
            var keyUpHookRestored = !keyUpHookWriteStarted;

            TryRestoreGroundItemHook(writer, allocator, Add(moduleBaseAddress, GroundItemKeyUpHookRva),
                ExpectedGroundItemKeyUpHook, keyUpHookWriteStarted, cleanupExceptions, ref keyUpHookRestored);
            TryRestoreGroundItemHook(writer, allocator, Add(moduleBaseAddress, GroundItemKeyDownHookRva),
                ExpectedGroundItemKeyDownHook, keyDownHookWriteStarted, cleanupExceptions, ref keyDownHookRestored);
            TryRestoreGroundItemHook(writer, allocator, Add(moduleBaseAddress, GroundItemFrameHookRva),
                ExpectedGroundItemFrameHook, frameHookWriteStarted, cleanupExceptions, ref frameHookRestored);
            TryRestoreGroundItemHook(writer, allocator, Add(moduleBaseAddress, GroundItemCollectorHookRva),
                ExpectedGroundItemCollectorHook, collectorHookWriteStarted, cleanupExceptions,
                ref collectorHookRestored);

            TryFreeGroundItemAllocation(allocator, keyUpStubAddress, keyUpHookRestored, cleanupExceptions);
            TryFreeGroundItemAllocation(allocator, keyDownStubAddress, keyDownHookRestored, cleanupExceptions);
            TryFreeGroundItemAllocation(allocator, frameStubAddress, frameHookRestored, cleanupExceptions);
            TryFreeGroundItemAllocation(allocator, collectorStubAddress, collectorHookRestored, cleanupExceptions);
            TryFreeGroundItemAllocation(allocator, stateAddress,
                collectorHookRestored && frameHookRestored && keyDownHookRestored && keyUpHookRestored,
                cleanupExceptions);

            var innerException = cleanupExceptions.Count == 0
                ? exception
                : new AggregateException([exception, .. cleanupExceptions]);
            throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", innerException);
        }
    }

    private static byte[] BuildGroundItemCollectorStub(IntPtr moduleBaseAddress, IntPtr stubAddress,
        IntPtr stateAddress)
    {
        var stub = GroundItemCollectorStubTemplate.ToArray();

        WriteUInt32(stub, 0x3A, checked((uint)Add(moduleBaseAddress, WorldItemVtableRva).ToInt64()));
        WriteUInt32(stub, 0x4A, checked((uint)stateAddress.ToInt64()));
        WriteInt32(stub, 0x4F, GroundItemCapacity);
        WriteUInt32(stub, 0x5A, checked((uint)Add(stateAddress, GroundItemStateEntryOffset).ToInt64()));
        WriteUInt32(stub, 0x70, checked((uint)stateAddress.ToInt64()));
        WriteUInt32(stub, 0x76, checked((uint)Add(stateAddress, 0x04).ToInt64()));
        WriteUInt32(stub, 0x7E, checked((uint)Add(stateAddress, 0x08).ToInt64()));
        WriteInt32(stub, 0x99, GetRelativeOffset(Add(stubAddress, 0x99), sizeof(int),
            Add(moduleBaseAddress, GroundItemCollectorHookRva + ExpectedGroundItemCollectorHook.Length)));

        return stub;
    }

    private static byte[] BuildGroundItemFrameStub(IntPtr moduleBaseAddress, IntPtr stubAddress,
        IntPtr stateAddress)
    {
        var stub = GroundItemFrameStubTemplate.ToArray();

        WriteUInt32(stub, 0x0D, checked((uint)Add(stateAddress, GroundItemStatePaneOffset).ToInt64()));
        WriteUInt32(stub, 0x13, checked((uint)stateAddress.ToInt64()));
        WriteInt32(stub, 0x26, GetRelativeOffset(Add(stubAddress, 0x26), sizeof(int),
            Add(moduleBaseAddress, InputGetEventManagerRva)));
        WriteUInt32(stub, 0x3B, checked((uint)stateAddress.ToInt64()));
        WriteUInt32(stub, 0x46, checked((uint)Add(stateAddress, GroundItemStateEntryOffset).ToInt64()));
        WriteUInt32(stub, 0x52, checked((uint)Add(moduleBaseAddress, WorldItemVtableRva).ToInt64()));
        WriteUInt32(stub, 0x79, checked((uint)Add(stateAddress, 0x04).ToInt64()));
        WriteUInt32(stub, 0x8D, checked((uint)Add(stateAddress, 0x08).ToInt64()));
        WriteInt32(stub, 0x92, GetRelativeOffset(Add(stubAddress, 0x92), sizeof(int),
            Add(moduleBaseAddress, RenderWorldObjectRva)));
        WriteInt32(stub, 0xB6, GetRelativeOffset(Add(stubAddress, 0xB6), sizeof(int),
            Add(moduleBaseAddress, GroundItemFrameHookRva + ExpectedGroundItemFrameHook.Length)));

        return stub;
    }

    private static byte[] BuildGroundItemKeyTransitionStub(IntPtr moduleBaseAddress, IntPtr stubAddress,
        IntPtr stateAddress, int hookRva, byte[] template)
    {
        var stub = template.ToArray();

        WriteUInt32(stub, 0x31, checked((uint)Add(stateAddress, GroundItemStatePaneOffset).ToInt64()));
        WriteInt32(stub, 0x3C, GetRelativeOffset(Add(stubAddress, 0x3C), sizeof(int),
            Add(moduleBaseAddress, UiPaneInvalidateRva)));
        WriteInt32(stub, 0x50, GetRelativeOffset(Add(stubAddress, 0x50), sizeof(int),
            Add(moduleBaseAddress, hookRva + ExpectedGroundItemKeyDownHook.Length)));

        return stub;
    }

    private static byte[] BuildGroundItemHook(IntPtr hookAddress, IntPtr stubAddress, int hookLength)
    {
        var hook = Enumerable.Repeat((byte)0x90, hookLength).ToArray();
        hook[0] = 0xE9;
        WriteInt32(hook, 1, GetRelativeOffset(hookAddress, 5, stubAddress));
        return hook;
    }

    private static void WriteGroundItemStub(BinaryWriter writer, IntPtr address, byte[] stub)
    {
        writer.BaseStream.Position = address.ToInt64();
        writer.Write(stub);
    }

    private static void ProtectGroundItemStub(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr address, byte[] stub)
    {
        allocator.MakeExecutable(address, stub.Length);
        VerifyRemoteBytes(writer, address, stub);
        allocator.FlushInstructionCache(address, stub.Length);
    }

    private static void WriteGroundItemHook(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr address, byte[] hook, ref bool writeStarted)
    {
        using (allocator.MakeWritable(address, hook.Length))
        {
            writeStarted = true;
            writer.BaseStream.Position = address.ToInt64();
            writer.Write(hook);
            allocator.FlushInstructionCache(address, hook.Length);
        }
    }

    private static void VerifyExpectedBytes(BinaryWriter writer, IntPtr address, byte[] expected)
    {
        var actual = ReadRemoteBytes(writer, address, expected.Length);
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidDataException($"Unexpected client bytes at 0x{address.ToInt64():X}: " +
                                           $"expected {Convert.ToHexString(expected)}, " +
                                           $"found {Convert.ToHexString(actual)}.");
        }
    }

    private static void TryRestoreGroundItemHook(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr hookAddress, byte[] expected, bool writeStarted, List<Exception> cleanupExceptions,
        ref bool restored)
    {
        if (!writeStarted)
        {
            return;
        }

        try
        {
            using (allocator.MakeWritable(hookAddress, expected.Length))
            {
                writer.BaseStream.Position = hookAddress.ToInt64();
                writer.Write(expected);
                allocator.FlushInstructionCache(hookAddress, expected.Length);
            }

            VerifyRemoteBytes(writer, hookAddress, expected);
            restored = true;
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }
    }

    private static void TryFreeGroundItemAllocation(ProcessMemoryAllocator allocator, IntPtr address,
        bool safeToFree, List<Exception> cleanupExceptions)
    {
        if (address == IntPtr.Zero || !safeToFree)
        {
            return;
        }

        try
        {
            allocator.FreeMemory(address);
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }
    }
}
