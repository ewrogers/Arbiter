using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Arbiter.Interop.Process;

namespace Arbiter.App.Services.Client;

public partial class GameClientService
{
    private const long SupportedClientSize = 3_112_960;
    private const string SupportedClientSha256 =
        "054A5D6ADC56099C6BFD9D2A58675AFF62DC788B63209A3D906492F5B89E96C6";

    private const int StuckModifierCallRva = 0x000A9D81;
    private const int InputGetEventManagerRva = 0x00027380;
    private const int InputPostKeyUpRva = 0x00066E60;
    private const int OriginalActivationFunctionRva = 0x000AC950;
    private const int GetMessageTimeThunkRva = 0x0022006E;

    private static readonly byte[] ExpectedStuckModifierCall = [0xE8, 0xCA, 0x2B, 0x00, 0x00];
    private const int PatchVerificationPadding = 8;
    private const int StuckModifierFixStubSize = 68;

    private static void VerifySupportedClient(string clientExecutablePath)
    {
        using var stream = File.OpenRead(clientExecutablePath);
        if (stream.Length != SupportedClientSize)
        {
            throw new InvalidDataException(
                $"Unsupported Dark Ages client size: expected {SupportedClientSize:N0} bytes, " +
                $"found {stream.Length:N0} bytes.");
        }

        var actualHash = SHA256.HashData(stream);
        if (!Convert.ToHexString(actualHash).Equals(SupportedClientSha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unsupported Dark Ages client hash: expected {SupportedClientSha256}, " +
                $"found {Convert.ToHexString(actualHash)}.");
        }
    }

    private static void ApplyStuckModifierFix(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr moduleBaseAddress)
    {
        var stage = "verify the modifier-fix call site";
        try
        {
            var callAddress = Add(moduleBaseAddress, StuckModifierCallRva);
            var patchWindowAddress = Add(callAddress, -PatchVerificationPadding);
            var originalPatchWindow = ReadRemoteBytes(writer, patchWindowAddress,
                PatchVerificationPadding + ExpectedStuckModifierCall.Length + PatchVerificationPadding);

            var actualCall = originalPatchWindow.AsSpan(PatchVerificationPadding, ExpectedStuckModifierCall.Length);
            if (!actualCall.SequenceEqual(ExpectedStuckModifierCall))
            {
                throw new InvalidDataException($"Unexpected client bytes at 0x{callAddress.ToInt64():X}: " +
                                               $"expected {Convert.ToHexString(ExpectedStuckModifierCall)}, " +
                                               $"found {Convert.ToHexString(actualCall)}.");
            }

            stage = "allocate the modifier-fix stub";
            var stubAddress = allocator.AllocMemory(_ => { }, StuckModifierFixStubSize);
            var stub = BuildStuckModifierFixStub(moduleBaseAddress, stubAddress);

            stage = "write the modifier-fix stub";
            writer.BaseStream.Position = stubAddress.ToInt64();
            writer.Write(stub);

            stage = "protect and verify the modifier-fix stub";
            allocator.MakeExecutable(stubAddress, stub.Length);
            VerifyRemoteBytes(writer, stubAddress, stub);

            var replacementCall = BuildStuckModifierCall(callAddress, stubAddress);

            stage = "write the modifier-fix call";
            using (allocator.MakeWritable(callAddress, replacementCall.Length))
            {
                writer.BaseStream.Position = callAddress.ToInt64();
                writer.Write(replacementCall);
            }

            stage = "verify the modifier-fix call";
            var expectedPatchWindow = originalPatchWindow.ToArray();
            replacementCall.CopyTo(expectedPatchWindow, PatchVerificationPadding);
            VerifyRemoteBytes(writer, patchWindowAddress, expectedPatchWindow);

            stage = "flush the modifier-fix instruction cache";
            allocator.FlushInstructionCache(stubAddress, stub.Length);
            allocator.FlushInstructionCache(callAddress, replacementCall.Length);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", exception);
        }
    }

    private static byte[] BuildStuckModifierCall(IntPtr callAddress, IntPtr stubAddress)
    {
        var call = new byte[ExpectedStuckModifierCall.Length];
        call[0] = 0xE8;
        WriteInt32(call, 1, GetRelativeOffset(callAddress, call.Length, stubAddress));
        return call;
    }

    private static byte[] BuildStuckModifierFixStub(IntPtr moduleBaseAddress, IntPtr stubAddress)
    {
        using var stream = new MemoryStream(StuckModifierFixStubSize);
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)0x9C); // PUSHFD
        writer.Write((byte)0x60); // PUSHAD

        writer.Write((byte)0xE8); // CALL input_get_event_manager
        writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
            Add(moduleBaseAddress, InputGetEventManagerRva)));

        writer.Write([0x85, 0xC0]); // TEST EAX, EAX
        writer.Write([0x74, 0x32]); // JZ cleanup complete
        writer.Write([0x89, 0xC3]); // MOV EBX, EAX

        writer.Write((byte)0xE8); // CALL GetMessageTime import thunk
        writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
            Add(moduleBaseAddress, GetMessageTimeThunkRva)));
        writer.Write([0x89, 0xC7]); // MOV EDI, EAX
        writer.Write([0x31, 0xF6]); // XOR ESI, ESI

        writer.Write([0xF6, 0x84, 0x33, 0x34, 0x03, 0x00, 0x00, 0x80]);
        writer.Write([0x74, 0x0D]); // JZ next scan code
        writer.Write([0x6A, 0x00]); // PUSH 0
        writer.Write((byte)0x57); // PUSH EDI
        writer.Write([0x6A, 0x00]); // PUSH 0
        writer.Write((byte)0x56); // PUSH ESI
        writer.Write([0x89, 0xD9]); // MOV ECX, EBX

        writer.Write((byte)0xE8); // CALL input_post_key_up
        writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
            Add(moduleBaseAddress, InputPostKeyUpRva)));

        writer.Write((byte)0x46); // INC ESI
        writer.Write([0x81, 0xFE, 0x00, 0x01, 0x00, 0x00]); // CMP ESI, 256
        writer.Write([0x7C, 0xE0]); // JL scan loop
        writer.Write([0xC6, 0x83, 0x34, 0x04, 0x00, 0x00, 0x00]); // MOV BYTE PTR [EBX + 0x434], 0

        writer.Write((byte)0x61); // POPAD
        writer.Write((byte)0x9D); // POPFD
        writer.Write((byte)0xE9); // JMP original activation function
        writer.Write(GetRelativeOffset(Add(stubAddress, checked((int)stream.Position)), sizeof(int),
            Add(moduleBaseAddress, OriginalActivationFunctionRva)));

        if (stream.Length != StuckModifierFixStubSize)
        {
            throw new InvalidOperationException(
                $"Unexpected stuck-modifier stub size: expected {StuckModifierFixStubSize}, found {stream.Length}.");
        }

        return stream.ToArray();
    }

    private static IntPtr Add(IntPtr address, int offset) => checked((IntPtr)(address.ToInt64() + offset));

    private static int GetRelativeOffset(IntPtr instructionAddress, int instructionSize, IntPtr targetAddress) =>
        checked((int)(targetAddress.ToInt64() - instructionAddress.ToInt64() - instructionSize));

    private static byte[] ReadRemoteBytes(BinaryWriter writer, IntPtr address, int size)
    {
        var bytes = new byte[size];
        writer.BaseStream.Position = address.ToInt64();
        writer.BaseStream.ReadExactly(bytes);
        return bytes;
    }

    private static void VerifyRemoteBytes(BinaryWriter writer, IntPtr address, byte[] expected)
    {
        var actual = ReadRemoteBytes(writer, address, expected.Length);
        if (!actual.SequenceEqual(expected))
        {
            throw new InvalidDataException($"Client memory verification failed at 0x{address.ToInt64():X}: " +
                                           $"expected {Convert.ToHexString(expected)}, " +
                                           $"found {Convert.ToHexString(actual)}.");
        }
    }

    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
