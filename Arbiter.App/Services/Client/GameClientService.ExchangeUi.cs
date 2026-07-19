using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arbiter.Interop.Process;

namespace Arbiter.App.Services.Client;

public partial class GameClientService
{
    private const int ExchangeDialogDraggableRva = 0x00069E33;
    private const int ExchangeCancelledHandlerRva = 0x0006A9E0;
    private const int ExchangeCancelledHandlerContinuationRva = 0x0006A9E5;
    private const int ExchangeCancelledAlertRva = 0x0006AA81;
    private const int ExchangeAcceptedHandlerRva = 0x0006AB20;
    private const int ExchangeAcceptedHandlerContinuationRva = 0x0006AB25;
    private const int ExchangeAcceptedAlertRva = 0x0006AC57;
    private const int FloatingPaletteAppendRva = 0x000803A0;
    private const int FloatingPaletteNewlineRva = 0x0028BC68;

    private const int ExchangeResultMessageMaxBytes = 130;
    private const int ExchangeResultStubAllocationSize = 256;

    private static readonly byte[] ExpectedExchangeDialogDraggable =
        [0xC7, 0x82, 0x2C, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] ExchangeDialogDraggableReplacement =
        [0xC7, 0x82, 0x2C, 0x06, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];
    private static readonly byte[] ExpectedExchangeResultHandlerHook = [0x55, 0x8B, 0xEC, 0x6A, 0xFF];
    private static readonly byte[] ExpectedExchangeCancelledAlert =
        [0x6A, 0x00, 0x68, 0x34, 0x06, 0x00, 0x00, 0xE8, 0x43, 0x9A, 0x04, 0x00];
    private static readonly byte[] ExpectedExchangeAcceptedAlert =
        [0x6A, 0x00, 0x68, 0x34, 0x06, 0x00, 0x00, 0xE8, 0x6D, 0x98, 0x04, 0x00];
    private static readonly byte[] SuppressExchangeAlertReplacement =
        [0x31, 0xC0, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90];

    private static void ApplyMakeExchangeDialogDraggablePatch(BinaryWriter writer,
        ProcessMemoryAllocator allocator, IntPtr moduleBaseAddress)
    {
        var address = Add(moduleBaseAddress, ExchangeDialogDraggableRva);
        VerifyOriginalBytes(writer, address, ExpectedExchangeDialogDraggable);
        WriteExecutableBytes(writer, allocator, address, ExchangeDialogDraggableReplacement);
        VerifyRemoteBytes(writer, address, ExchangeDialogDraggableReplacement);
    }

    private static void ApplyShowExchangeResultsInMessageBarPatch(BinaryWriter writer,
        ProcessMemoryAllocator allocator, IntPtr moduleBaseAddress)
    {
        var stage = "verify the exchange result hooks";
        var cancelledStubAddress = IntPtr.Zero;
        var acceptedStubAddress = IntPtr.Zero;
        var writesStarted = false;

        var cancelledHookAddress = Add(moduleBaseAddress, ExchangeCancelledHandlerRva);
        var acceptedHookAddress = Add(moduleBaseAddress, ExchangeAcceptedHandlerRva);
        var cancelledAlertAddress = Add(moduleBaseAddress, ExchangeCancelledAlertRva);
        var acceptedAlertAddress = Add(moduleBaseAddress, ExchangeAcceptedAlertRva);

        try
        {
            VerifyOriginalBytes(writer, cancelledHookAddress, ExpectedExchangeResultHandlerHook);
            VerifyOriginalBytes(writer, acceptedHookAddress, ExpectedExchangeResultHandlerHook);
            VerifyOriginalBytes(writer, cancelledAlertAddress, ExpectedExchangeCancelledAlert);
            VerifyOriginalBytes(writer, acceptedAlertAddress, ExpectedExchangeAcceptedAlert);

            stage = "allocate the exchange result stubs";
            cancelledStubAddress = allocator.AllocMemory(_ => { }, ExchangeResultStubAllocationSize);
            acceptedStubAddress = allocator.AllocMemory(_ => { }, ExchangeResultStubAllocationSize);
            var cancelledStub = BuildExchangeResultHandlerStub(moduleBaseAddress, cancelledStubAddress,
                accepted: false);
            var acceptedStub = BuildExchangeResultHandlerStub(moduleBaseAddress, acceptedStubAddress,
                accepted: true);

            stage = "write the exchange result stubs";
            WriteExecutableStub(writer, allocator, cancelledStubAddress, cancelledStub);
            WriteExecutableStub(writer, allocator, acceptedStubAddress, acceptedStub);

            var cancelledHook = BuildExchangeEntryHook(cancelledHookAddress, cancelledStubAddress);
            var acceptedHook = BuildExchangeEntryHook(acceptedHookAddress, acceptedStubAddress);

            stage = "write the exchange result hooks";
            writesStarted = true;
            WriteExecutableBytes(writer, allocator, cancelledAlertAddress, SuppressExchangeAlertReplacement);
            WriteExecutableBytes(writer, allocator, acceptedAlertAddress, SuppressExchangeAlertReplacement);
            WriteExecutableBytes(writer, allocator, cancelledHookAddress, cancelledHook);
            WriteExecutableBytes(writer, allocator, acceptedHookAddress, acceptedHook);

            stage = "verify the exchange result hooks";
            VerifyRemoteBytes(writer, cancelledAlertAddress, SuppressExchangeAlertReplacement);
            VerifyRemoteBytes(writer, acceptedAlertAddress, SuppressExchangeAlertReplacement);
            VerifyRemoteBytes(writer, cancelledHookAddress, cancelledHook);
            VerifyRemoteBytes(writer, acceptedHookAddress, acceptedHook);
        }
        catch (Exception exception)
        {
            var cleanupExceptions = new List<Exception>();
            if (writesStarted)
            {
                TryRestoreExecutableBytes(writer, allocator, cancelledHookAddress,
                    ExpectedExchangeResultHandlerHook, cleanupExceptions);
                TryRestoreExecutableBytes(writer, allocator, acceptedHookAddress,
                    ExpectedExchangeResultHandlerHook, cleanupExceptions);
                TryRestoreExecutableBytes(writer, allocator, cancelledAlertAddress,
                    ExpectedExchangeCancelledAlert, cleanupExceptions);
                TryRestoreExecutableBytes(writer, allocator, acceptedAlertAddress,
                    ExpectedExchangeAcceptedAlert, cleanupExceptions);
            }

            if (cleanupExceptions.Count == 0)
            {
                TryFreeMemory(allocator, cancelledStubAddress, cleanupExceptions);
                TryFreeMemory(allocator, acceptedStubAddress, cleanupExceptions);
            }

            throw BuildPatchException(stage, exception, cleanupExceptions);
        }
    }

    private static byte[] BuildExchangeResultHandlerStub(IntPtr moduleBaseAddress, IntPtr stubAddress,
        bool accepted)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write([0x55, 0x89, 0xE5]); // PUSH EBP; MOV EBP, ESP
        writer.Write([0x81, 0xEC, 0x88, 0x00, 0x00, 0x00]); // SUB ESP, 0x88
        writer.Write([0x53, 0x56, 0x57]); // PUSH EBX; PUSH ESI; PUSH EDI
        writer.Write([0x89, 0x4D, 0xFC]); // MOV [EBP - 4], ECX
        writer.Write([0x8B, 0x5D, 0x08]); // MOV EBX, [EBP + 8]

        var skipMessageJumps = new List<int>();
        if (accepted)
        {
            writer.Write([0x80, 0x7B, 0x02, 0x00]); // CMP BYTE PTR [EBX + 2], 0
            var nonLocalPartyJump = WriteNearJumpPlaceholder(writer, 0x85); // JNE non-local party

            writer.Write([0x8B, 0x45, 0xFC]); // MOV EAX, [EBP - 4]
            writer.Write([0x80, 0xB8, 0x36, 0x06, 0x00, 0x00, 0x01]); // CMP [EAX + 0x636], 1
            skipMessageJumps.Add(WriteNearJumpPlaceholder(writer, 0x85));
            var displayMessageJump = WriteNearJumpPlaceholder(writer, null);

            var nonLocalPartyOffset = checked((int)stream.Position);
            PatchLocalBranch(stream, nonLocalPartyJump, nonLocalPartyOffset);
            writer.Write([0x8B, 0x45, 0xFC]); // MOV EAX, [EBP - 4]
            writer.Write([0x80, 0xB8, 0x35, 0x06, 0x00, 0x00, 0x01]); // CMP [EAX + 0x635], 1
            skipMessageJumps.Add(WriteNearJumpPlaceholder(writer, 0x85));

            PatchLocalBranch(stream, displayMessageJump, checked((int)stream.Position));
        }

        writer.Write([0x0F, 0xB6, 0x4B, 0x03]); // MOVZX ECX, BYTE PTR [EBX + 3]
        writer.Write([0x81, 0xF9, 0x82, 0x00, 0x00, 0x00]); // CMP ECX, 130
        writer.Write([0x76, 0x05]); // JBE copy message
        writer.Write([0xB9, 0x82, 0x00, 0x00, 0x00]); // MOV ECX, 130
        writer.Write([0x8D, 0x73, 0x04]); // LEA ESI, [EBX + 4]
        writer.Write([0x8D, 0xBD, 0x78, 0xFF, 0xFF, 0xFF]); // LEA EDI, [EBP - 0x88]
        writer.Write([0xFC, 0xF3, 0xA4]); // CLD; REP MOVSB
        writer.Write([0xC6, 0x07, 0x00]); // MOV BYTE PTR [EDI], 0

        writer.Write([0x6A, 0x58]); // PUSH palette 0x58
        writer.Write([0x8D, 0x85, 0x78, 0xFF, 0xFF, 0xFF]); // LEA EAX, [EBP - 0x88]
        writer.Write((byte)0x50); // PUSH EAX
        WriteRelativeCall(writer, stubAddress, Add(moduleBaseAddress, FloatingPaletteAppendRva));
        writer.Write([0x83, 0xC4, 0x08]); // ADD ESP, 8

        writer.Write([0x6A, 0x58]); // PUSH palette 0x58
        writer.Write((byte)0x68); // PUSH normal newline
        WriteAddress(writer, Add(moduleBaseAddress, FloatingPaletteNewlineRva));
        WriteRelativeCall(writer, stubAddress, Add(moduleBaseAddress, FloatingPaletteAppendRva));
        writer.Write([0x83, 0xC4, 0x08]); // ADD ESP, 8

        var skipMessageOffset = checked((int)stream.Position);
        foreach (var jump in skipMessageJumps)
        {
            PatchLocalBranch(stream, jump, skipMessageOffset);
        }

        writer.Write([0x5F, 0x5E, 0x5B]); // POP EDI; POP ESI; POP EBX
        writer.Write([0x8B, 0x4D, 0xFC]); // MOV ECX, [EBP - 4]
        writer.Write([0x89, 0xEC, 0x5D]); // MOV ESP, EBP; POP EBP
        writer.Write(ExpectedExchangeResultHandlerHook);
        WriteRelativeJump(writer, stubAddress, Add(moduleBaseAddress,
            accepted ? ExchangeAcceptedHandlerContinuationRva : ExchangeCancelledHandlerContinuationRva));

        return stream.ToArray();
    }

    private static byte[] BuildExchangeEntryHook(IntPtr hookAddress, IntPtr stubAddress)
    {
        var hook = new byte[ExpectedExchangeResultHandlerHook.Length];
        hook[0] = 0xE9;
        WriteInt32(hook, 1, GetRelativeOffset(hookAddress, hook.Length, stubAddress));
        return hook;
    }

    private static void VerifyOriginalBytes(BinaryWriter writer, IntPtr address, byte[] expected)
    {
        var actual = ReadRemoteBytes(writer, address, expected.Length);
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidDataException($"Unexpected client bytes at 0x{address.ToInt64():X}: " +
                                           $"expected {Convert.ToHexString(expected)}, " +
                                           $"found {Convert.ToHexString(actual)}.");
        }
    }

    private static void WriteExecutableStub(BinaryWriter writer, ProcessMemoryAllocator allocator, IntPtr address,
        byte[] stub)
    {
        writer.BaseStream.Position = address.ToInt64();
        writer.Write(stub);
        allocator.MakeExecutable(address, stub.Length);
        VerifyRemoteBytes(writer, address, stub);
        allocator.FlushInstructionCache(address, stub.Length);
    }

    private static void WriteExecutableBytes(BinaryWriter writer, ProcessMemoryAllocator allocator, IntPtr address,
        byte[] bytes)
    {
        using (allocator.MakeWritable(address, bytes.Length))
        {
            writer.BaseStream.Position = address.ToInt64();
            writer.Write(bytes);
        }

        allocator.FlushInstructionCache(address, bytes.Length);
    }

    private static void TryRestoreExecutableBytes(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr address, byte[] original, List<Exception> cleanupExceptions)
    {
        try
        {
            WriteExecutableBytes(writer, allocator, address, original);
            VerifyRemoteBytes(writer, address, original);
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }
    }

    private static void TryFreeMemory(ProcessMemoryAllocator allocator, IntPtr address,
        List<Exception> cleanupExceptions)
    {
        if (address == IntPtr.Zero)
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

    private static InvalidOperationException BuildPatchException(string stage, Exception exception,
        List<Exception> cleanupExceptions)
    {
        var innerException = cleanupExceptions.Count == 0
            ? exception
            : new AggregateException([exception, .. cleanupExceptions]);
        return new InvalidOperationException($"Failed to {stage}: {exception.Message}", innerException);
    }

    private static void WriteRelativeCall(BinaryWriter writer, IntPtr stubAddress, IntPtr targetAddress)
    {
        var instructionOffset = checked((int)writer.BaseStream.Position);
        writer.Write((byte)0xE8);
        writer.Write(GetRelativeOffset(Add(stubAddress, instructionOffset), 5, targetAddress));
    }

    private static void WriteRelativeJump(BinaryWriter writer, IntPtr stubAddress, IntPtr targetAddress)
    {
        var instructionOffset = checked((int)writer.BaseStream.Position);
        writer.Write((byte)0xE9);
        writer.Write(GetRelativeOffset(Add(stubAddress, instructionOffset), 5, targetAddress));
    }

    private static int WriteNearJumpPlaceholder(BinaryWriter writer, byte? condition)
    {
        if (condition is null)
        {
            writer.Write((byte)0xE9);
        }
        else
        {
            writer.Write((byte)0x0F);
            writer.Write(condition.Value);
        }

        var operandOffset = checked((int)writer.BaseStream.Position);
        writer.Write(0);
        return operandOffset;
    }

    private static void PatchLocalBranch(MemoryStream stream, int operandOffset, int targetOffset)
    {
        var returnPosition = stream.Position;
        stream.Position = operandOffset;
        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(checked(targetOffset - operandOffset - sizeof(int)));
        }

        stream.Position = returnPosition;
    }

    private static void WriteAddress(BinaryWriter writer, IntPtr address) =>
        writer.Write(checked((uint)address.ToInt64()));
}
