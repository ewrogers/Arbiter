using System;
using System.Collections.Generic;
using System.IO;
using Arbiter.Interop.Process;

namespace Arbiter.App.Services.Client;

public partial class GameClientService
{
    private const int AutoFollowStateSize = 0x18;
    private const int AutoFollowActiveDistanceOffset = 0x0C;
    private const int AutoFollowShiftDistanceOffset = 0x10;
    private const int AutoFollowDefaultDistance = 3;

    private const int AutoFollowSelectHookRva = 0x001EF33A;
    private const int AutoFollowShiftDispatchHookRva = 0x001D48DF;
    private const int AutoFollowRouteHookRva = 0x001F49E5;
    private const int AutoFollowGenerationHookRva = 0x001F4AA2;
    private const int AutoFollowAttackHookRva = 0x001F4D0E;

    private const int AutoFollowSelectStubOffset = 0x000;
    private const int AutoFollowAttackStubOffset = 0x044;
    private const int AutoFollowGenerationStubOffset = 0x072;
    private const int AutoFollowRouteStubOffset = 0x0A8;
    private const int AutoFollowGenerationReplayStubOffset = 0x190;
    private const int AutoFollowShiftDispatchStubOffset = 0x19B;

    private const int AutoFollowPursuitRva = 0x001F4A70;
    private const int AutoFollowSendAttackRva = 0x001F44B0;
    private const int AutoFollowGenerationNonZeroRva = 0x001F4AAD;
    private const int AutoFollowGenerationZeroRva = 0x001F4AA8;
    private const int AutoFollowResetMovementRva = 0x001F4900;
    private const int AutoFollowRouteEpilogueRva = 0x001F4A5B;
    private const int AutoFollowRouteContinuationRva = 0x001F49EF;
    private const int AutoFollowShiftDispatchAllowedRva = 0x001D48F1;
    private const int AutoFollowShiftDispatchNativeFilterRva = 0x001D48E5;

    private static readonly byte[] ExpectedAutoFollowSelectHook = [0xE8, 0x31, 0x57, 0x00, 0x00];
    private static readonly byte[] ExpectedAutoFollowShiftDispatchHook = [0x83, 0x7D, 0xA0, 0x08, 0x74, 0x0C];
    private static readonly byte[] ExpectedAutoFollowRouteHook =
        [0x8B, 0x45, 0xE0, 0x83, 0xB8, 0xB8, 0x02, 0x00, 0x00, 0x00];
    private static readonly byte[] ExpectedAutoFollowGenerationHook = [0x83, 0x7D, 0x08, 0x00, 0x75, 0x05];
    private static readonly byte[] ExpectedAutoFollowAttackHook = [0xE8, 0x9D, 0xF7, 0xFF, 0xFF];

    private static readonly byte[] AutoFollowStubTemplate = Convert.FromHexString(
        "8B450CF6402C04742F890D000000008B442404A300000000A100000000A300000000A000000000A200000000C6050000000001E900000000C6050000000000E9" +
        "00000000803D00000000007420390D0000000075188B81C80200003B0500000000750A803D00000000007501C3E900000000803D000000000074248B45B43B05" +
        "0000000075198B45083B0500000000750E8B45B48B80C8020000A300000000837D0800E900000000803D0000000000745E8B45E03B050000000075538B90C802" +
        "00003B150000000074198B0D000000004139CA753A8B0D000000003988BC020000752C8B90B802000083C2018B0D0000000083F9017D05B90100000039CA7F0F" +
        "6A018B4DE0E800000000E9000000008B45E083B8B802000000E9000000005589E58B4D08890D000000008B450CA3000000008B551083FA017D05BA0100000081" +
        "FAFF0000007E05BAFF0000008915000000008B451485C00F95C0A200000000C6050000000001FF750C8B4D08E80000000089EC5DC21000C60500000000008B4C" +
        "24046A00E800000000C2040000000000" +
        "0F8500000000E900000000" +
        "837DA0080F8400000000807DC3000F84000000008B450C80780C050F8500000000837DA0010F8400000000837DA0020F8400000000E900000000");

    private static void ApplyImprovedAutoFollowPatch(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr moduleBaseAddress)
    {
        var stage = "verify the improved auto-follow hooks";
        var stateAddress = IntPtr.Zero;
        var stubAddress = IntPtr.Zero;
        var selectHookWriteStarted = false;
        var shiftDispatchHookWriteStarted = false;
        var routeHookWriteStarted = false;
        var generationHookWriteStarted = false;
        var attackHookWriteStarted = false;

        try
        {
            var selectHookAddress = Add(moduleBaseAddress, AutoFollowSelectHookRva);
            var shiftDispatchHookAddress = Add(moduleBaseAddress, AutoFollowShiftDispatchHookRva);
            var routeHookAddress = Add(moduleBaseAddress, AutoFollowRouteHookRva);
            var generationHookAddress = Add(moduleBaseAddress, AutoFollowGenerationHookRva);
            var attackHookAddress = Add(moduleBaseAddress, AutoFollowAttackHookRva);

            VerifyExpectedBytes(writer, selectHookAddress, ExpectedAutoFollowSelectHook);
            VerifyExpectedBytes(writer, shiftDispatchHookAddress, ExpectedAutoFollowShiftDispatchHook);
            VerifyExpectedBytes(writer, routeHookAddress, ExpectedAutoFollowRouteHook);
            VerifyExpectedBytes(writer, generationHookAddress, ExpectedAutoFollowGenerationHook);
            VerifyExpectedBytes(writer, attackHookAddress, ExpectedAutoFollowAttackHook);

            stage = "allocate the improved auto-follow state and stub";
            var expectedState = BuildImprovedAutoFollowState();
            stateAddress = allocator.AllocMemory(stateWriter => stateWriter.Write(expectedState),
                expectedState.Length);
            stubAddress = allocator.AllocMemory(_ => { }, AutoFollowStubTemplate.Length);

            var stub = BuildImprovedAutoFollowStub(moduleBaseAddress, stubAddress, stateAddress);

            stage = "write the improved auto-follow stub";
            writer.BaseStream.Position = stubAddress.ToInt64();
            writer.Write(stub);

            stage = "protect and verify the improved auto-follow state and stub";
            allocator.MakeExecutable(stubAddress, stub.Length);
            VerifyRemoteBytes(writer, stateAddress, expectedState);
            VerifyRemoteBytes(writer, stubAddress, stub);
            allocator.FlushInstructionCache(stubAddress, stub.Length);

            var selectHook = BuildImprovedAutoFollowHook(selectHookAddress, stubAddress,
                AutoFollowSelectStubOffset, 0xE8, ExpectedAutoFollowSelectHook.Length);
            var shiftDispatchHook = BuildImprovedAutoFollowHook(shiftDispatchHookAddress, stubAddress,
                AutoFollowShiftDispatchStubOffset, 0xE9, ExpectedAutoFollowShiftDispatchHook.Length);
            var routeHook = BuildImprovedAutoFollowHook(routeHookAddress, stubAddress,
                AutoFollowRouteStubOffset, 0xE9, ExpectedAutoFollowRouteHook.Length);
            var generationHook = BuildImprovedAutoFollowHook(generationHookAddress, stubAddress,
                AutoFollowGenerationStubOffset, 0xE9, ExpectedAutoFollowGenerationHook.Length);
            var attackHook = BuildImprovedAutoFollowHook(attackHookAddress, stubAddress,
                AutoFollowAttackStubOffset, 0xE8, ExpectedAutoFollowAttackHook.Length);

            stage = "write the improved auto-follow selection hook";
            WriteImprovedAutoFollowHook(writer, allocator, selectHookAddress, selectHook,
                ref selectHookWriteStarted);

            stage = "write the improved auto-follow route hook";
            WriteImprovedAutoFollowHook(writer, allocator, routeHookAddress, routeHook,
                ref routeHookWriteStarted);

            stage = "write the improved auto-follow generation hook";
            WriteImprovedAutoFollowHook(writer, allocator, generationHookAddress, generationHook,
                ref generationHookWriteStarted);

            stage = "write the improved auto-follow attack hook";
            WriteImprovedAutoFollowHook(writer, allocator, attackHookAddress, attackHook,
                ref attackHookWriteStarted);

            stage = "write the improved auto-follow Shift dispatch hook";
            WriteImprovedAutoFollowHook(writer, allocator, shiftDispatchHookAddress, shiftDispatchHook,
                ref shiftDispatchHookWriteStarted);

            stage = "verify the improved auto-follow hooks";
            VerifyRemoteBytes(writer, selectHookAddress, selectHook);
            VerifyRemoteBytes(writer, shiftDispatchHookAddress, shiftDispatchHook);
            VerifyRemoteBytes(writer, routeHookAddress, routeHook);
            VerifyRemoteBytes(writer, generationHookAddress, generationHook);
            VerifyRemoteBytes(writer, attackHookAddress, attackHook);
        }
        catch (Exception exception)
        {
            var cleanupExceptions = new List<Exception>();
            var selectHookRestored = !selectHookWriteStarted;
            var shiftDispatchHookRestored = !shiftDispatchHookWriteStarted;
            var routeHookRestored = !routeHookWriteStarted;
            var generationHookRestored = !generationHookWriteStarted;
            var attackHookRestored = !attackHookWriteStarted;

            TryRestoreImprovedAutoFollowHook(writer, allocator,
                Add(moduleBaseAddress, AutoFollowShiftDispatchHookRva),
                ExpectedAutoFollowShiftDispatchHook, shiftDispatchHookWriteStarted, cleanupExceptions,
                ref shiftDispatchHookRestored);
            TryRestoreImprovedAutoFollowHook(writer, allocator, Add(moduleBaseAddress, AutoFollowAttackHookRva),
                ExpectedAutoFollowAttackHook, attackHookWriteStarted, cleanupExceptions, ref attackHookRestored);
            TryRestoreImprovedAutoFollowHook(writer, allocator, Add(moduleBaseAddress, AutoFollowGenerationHookRva),
                ExpectedAutoFollowGenerationHook, generationHookWriteStarted, cleanupExceptions,
                ref generationHookRestored);
            TryRestoreImprovedAutoFollowHook(writer, allocator, Add(moduleBaseAddress, AutoFollowRouteHookRva),
                ExpectedAutoFollowRouteHook, routeHookWriteStarted, cleanupExceptions, ref routeHookRestored);
            TryRestoreImprovedAutoFollowHook(writer, allocator, Add(moduleBaseAddress, AutoFollowSelectHookRva),
                ExpectedAutoFollowSelectHook, selectHookWriteStarted, cleanupExceptions, ref selectHookRestored);

            var allHooksRestored = selectHookRestored && shiftDispatchHookRestored && routeHookRestored &&
                                   generationHookRestored && attackHookRestored;
            TryFreeImprovedAutoFollowAllocation(allocator, stubAddress, allHooksRestored, cleanupExceptions);
            TryFreeImprovedAutoFollowAllocation(allocator, stateAddress, allHooksRestored, cleanupExceptions);

            var innerException = cleanupExceptions.Count == 0
                ? exception
                : new AggregateException([exception, .. cleanupExceptions]);
            throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", innerException);
        }
    }

    private static byte[] BuildImprovedAutoFollowState()
    {
        var state = new byte[AutoFollowStateSize];
        WriteInt32(state, AutoFollowActiveDistanceOffset, AutoFollowDefaultDistance);
        WriteInt32(state, AutoFollowShiftDistanceOffset, AutoFollowDefaultDistance);
        return state;
    }

    private static byte[] BuildImprovedAutoFollowStub(IntPtr moduleBaseAddress, IntPtr stubAddress,
        IntPtr stateAddress)
    {
        var stub = new byte[AutoFollowStubTemplate.Length];
        AutoFollowStubTemplate.CopyTo(stub, 0);

        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x00), 0x00B, 0x04F, 0x080, 0x0B6, 0x126);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x04), 0x014, 0x08B, 0x0D7, 0x12E);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x08), 0x05D, 0x09B, 0x0C4, 0x0CC);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x0C), 0x01E, 0x0EE, 0x14E);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x10), 0x019);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x14),
            0x02E, 0x03A, 0x046, 0x074, 0x0AA, 0x161, 0x179);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x15), 0x028, 0x065, 0x15B);
        WriteImprovedAutoFollowAddresses(stub, Add(stateAddress, 0x16), 0x023);

        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowPursuitRva), 0x034, 0x040, 0x16D);
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowSendAttackRva), 0x06E);

        // The documented 0x005F4AA6 continuation is inside the replaced generation hook.
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(stubAddress, AutoFollowGenerationReplayStubOffset), 0x0A4);
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowGenerationNonZeroRva), 0x192);
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowGenerationZeroRva), 0x197);

        // Admit only the Shift-modified pursuit gesture for players and monsters.
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowShiftDispatchAllowedRva), 0x1A1, 0x1C2, 0x1CC);
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowShiftDispatchNativeFilterRva), 0x1AB, 0x1B8, 0x1D1);

        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowResetMovementRva), 0x106, 0x185);
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowRouteEpilogueRva), 0x10B);
        WriteImprovedAutoFollowRelativeOffsets(stub, stubAddress,
            Add(moduleBaseAddress, AutoFollowRouteContinuationRva), 0x11A);

        return stub;
    }

    private static byte[] BuildImprovedAutoFollowHook(IntPtr hookAddress, IntPtr stubAddress,
        int stubOffset, byte opcode, int hookLength)
    {
        var hook = new byte[hookLength];
        Array.Fill(hook, (byte)0x90);
        hook[0] = opcode;
        WriteInt32(hook, 1, GetRelativeOffset(hookAddress, 5, Add(stubAddress, stubOffset)));
        return hook;
    }

    private static void WriteImprovedAutoFollowAddresses(byte[] stub, IntPtr address, params int[] offsets)
    {
        var absoluteAddress = checked((uint)address.ToInt64());
        foreach (var offset in offsets)
        {
            WriteUInt32(stub, offset, absoluteAddress);
        }
    }

    private static void WriteImprovedAutoFollowRelativeOffsets(byte[] stub, IntPtr stubAddress,
        IntPtr targetAddress, params int[] offsets)
    {
        foreach (var offset in offsets)
        {
            WriteInt32(stub, offset,
                GetRelativeOffset(Add(stubAddress, offset), sizeof(int), targetAddress));
        }
    }

    private static void WriteImprovedAutoFollowHook(BinaryWriter writer, ProcessMemoryAllocator allocator,
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

    private static void TryRestoreImprovedAutoFollowHook(BinaryWriter writer, ProcessMemoryAllocator allocator,
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

    private static void TryFreeImprovedAutoFollowAllocation(ProcessMemoryAllocator allocator, IntPtr address,
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
