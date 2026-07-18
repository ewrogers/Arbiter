using System;
using System.IO;
using System.Linq;
using Arbiter.Interop.Process;

namespace Arbiter.App.Services.Client;

public partial class GameClientService
{
    private const int DialogItemLabelMaxBytes = 20;
    private const int ItemQuantityDialogHookRva = 0x0013609C;
    private const int DialogItemNameCopyRva = 0x002228C4;
    private const int DialogUiGetGuiBackPaneRva = 0x001A9C40;
    private const int UiTextTruncateDbcsSafeRva = 0x0007D670;
    private const int WsprintfImportAddressRva = 0x00269380;

    private static readonly byte[] ExpectedItemQuantityDialogHook = [0xE8, 0x23, 0xC8, 0x0E, 0x00];
    private static readonly byte[] ShowItemQuantityInDialogsStubTemplate =
    [
        0x55, 0x89, 0xE5, 0x53, 0x56, 0x57, 0x81, 0xEC, 0x20, 0x01, 0x00, 0x00, 0x8B, 0x75, 0x10, 0x89,
        0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0x8B, 0x7D, 0x00, 0x8B, 0x7F, 0xE8, 0x0F, 0xB6, 0x3F, 0x90, 0x90,
        0x90, 0x90, 0x90, 0x83, 0xFF, 0x01, 0x0F, 0x82, 0x0D, 0x01, 0x00, 0x00, 0x83, 0xFF, 0x3C, 0x0F,
        0x87, 0x04, 0x01, 0x00, 0x00, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x85, 0xC0, 0x0F, 0x84, 0xF7, 0x00,
        0x00, 0x00, 0x8B, 0x80, 0x88, 0x4F, 0x00, 0x00, 0x85, 0xC0, 0x0F, 0x84, 0xE9, 0x00, 0x00, 0x00,
        0x8B, 0x84, 0xB8, 0x9C, 0x01, 0x00, 0x00, 0x85, 0xC0, 0x0F, 0x84, 0xDA, 0x00, 0x00, 0x00, 0x80,
        0xB8, 0x44, 0x02, 0x00, 0x00, 0x00, 0x0F, 0x84, 0xCD, 0x00, 0x00, 0x00, 0x8B, 0x88, 0x40, 0x02,
        0x00, 0x00, 0x49, 0x0F, 0x8E, 0xC0, 0x00, 0x00, 0x00, 0x41, 0x89, 0x8D, 0xD4, 0xFE, 0xFF, 0xFF,
        0xE8, 0x06, 0x00, 0x00, 0x00, 0x20, 0x28, 0x25, 0x75, 0x29, 0x00, 0x58, 0xFF, 0xB5, 0xD4, 0xFE,
        0xFF, 0xFF, 0x50, 0x8D, 0x45, 0xE4, 0x50, 0xFF, 0x15, 0x00, 0x00, 0x00, 0x00, 0x83, 0xC4, 0x0C,
        0x83, 0xF8, 0x01, 0x0F, 0x8E, 0x90, 0x00, 0x00, 0x00, 0x83, 0xF8, 0x0F, 0x0F, 0x83, 0x87, 0x00,
        0x00, 0x00, 0x89, 0x85, 0xE0, 0xFE, 0xFF, 0xFF, 0x31, 0xC9, 0x81, 0xF9, 0x00, 0x01, 0x00, 0x00,
        0x73, 0x09, 0x80, 0x3C, 0x0E, 0x00, 0x74, 0x03, 0x41, 0xEB, 0xEF, 0xBA, 0x00, 0x00, 0x00, 0x00,
        0x2B, 0x95, 0xE0, 0xFE, 0xFF, 0xFF, 0x39, 0xD1, 0x76, 0x3D, 0x83, 0xEA, 0x02, 0x8D, 0xBD, 0xE4,
        0xFE, 0xFF, 0xFF, 0x89, 0xD1, 0xFC, 0xF3, 0xA4, 0xC6, 0x07, 0x00, 0x52, 0x8D, 0x85, 0xE4, 0xFE,
        0xFF, 0xFF, 0x50, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x83, 0xC4, 0x08, 0x8D, 0xBD, 0xE4, 0xFE, 0xFF,
        0xFF, 0x80, 0x3F, 0x00, 0x74, 0x03, 0x47, 0xEB, 0xF8, 0x66, 0xC7, 0x07, 0x2E, 0x2E, 0x90, 0x90,
        0x90, 0x90, 0x83, 0xC7, 0x02, 0xEB, 0x09, 0x8D, 0xBD, 0xE4, 0xFE, 0xFF, 0xFF, 0xFC, 0xF3, 0xA4,
        0x8D, 0x75, 0xE4, 0x8B, 0x8D, 0xE0, 0xFE, 0xFF, 0xFF, 0x41, 0xFC, 0xF3, 0xA4, 0x8D, 0x85, 0xE4,
        0xFE, 0xFF, 0xFF, 0x89, 0x85, 0xD8, 0xFE, 0xFF, 0xFF, 0xFF, 0xB5, 0xD8, 0xFE, 0xFF, 0xFF, 0xFF,
        0x75, 0x0C, 0xFF, 0x75, 0x08, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x83, 0xC4, 0x0C, 0x8D, 0x65, 0xF4,
        0x5F, 0x5E, 0x5B, 0x5D, 0xC3,
    ];

    private static void ApplyShowItemQuantityInDialogsPatch(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr moduleBaseAddress)
    {
        var stage = "verify the dialog item-quantity hook";
        var stubAddress = IntPtr.Zero;
        var hookWriteStarted = false;

        try
        {
            var hookAddress = Add(moduleBaseAddress, ItemQuantityDialogHookRva);
            var actualHook = ReadRemoteBytes(writer, hookAddress, ExpectedItemQuantityDialogHook.Length);
            if (!actualHook.SequenceEqual(ExpectedItemQuantityDialogHook))
            {
                throw new InvalidDataException($"Unexpected client bytes at 0x{hookAddress.ToInt64():X}: " +
                                               $"expected {Convert.ToHexString(ExpectedItemQuantityDialogHook)}, " +
                                               $"found {Convert.ToHexString(actualHook)}.");
            }

            stage = "allocate the dialog item-quantity stub";
            stubAddress = allocator.AllocMemory(_ => { }, ShowItemQuantityInDialogsStubTemplate.Length);
            var stub = BuildShowItemQuantityInDialogsStub(moduleBaseAddress, stubAddress);

            stage = "write the dialog item-quantity stub";
            writer.BaseStream.Position = stubAddress.ToInt64();
            writer.Write(stub);

            stage = "protect and verify the dialog item-quantity stub";
            allocator.MakeExecutable(stubAddress, stub.Length);
            VerifyRemoteBytes(writer, stubAddress, stub);
            allocator.FlushInstructionCache(stubAddress, stub.Length);

            var replacementHook = BuildShowItemQuantityInDialogsHook(hookAddress, stubAddress);

            stage = "write the dialog item-quantity hook";
            using (allocator.MakeWritable(hookAddress, replacementHook.Length))
            {
                hookWriteStarted = true;
                writer.BaseStream.Position = hookAddress.ToInt64();
                writer.Write(replacementHook);
                allocator.FlushInstructionCache(hookAddress, replacementHook.Length);
            }

            stage = "verify the dialog item-quantity hook";
            VerifyRemoteBytes(writer, hookAddress, replacementHook);
        }
        catch (Exception exception)
        {
            Exception? cleanupException = null;
            var hookRestored = !hookWriteStarted;
            var hookAddress = Add(moduleBaseAddress, ItemQuantityDialogHookRva);

            if (hookWriteStarted)
            {
                try
                {
                    RestoreItemQuantityDialogHook(writer, allocator, hookAddress);
                    hookRestored = true;
                }
                catch (Exception rollbackException)
                {
                    cleanupException = rollbackException;
                }
            }

            if (stubAddress != IntPtr.Zero && hookRestored)
            {
                try
                {
                    allocator.FreeMemory(stubAddress);
                }
                catch (Exception freeException)
                {
                    cleanupException = freeException;
                }
            }

            var innerException = cleanupException is null
                ? exception
                : new AggregateException(exception, cleanupException);
            throw new InvalidOperationException($"Failed to {stage}: {exception.Message}", innerException);
        }
    }

    private static byte[] BuildShowItemQuantityInDialogsStub(IntPtr moduleBaseAddress, IntPtr stubAddress)
    {
        var stub = ShowItemQuantityInDialogsStubTemplate.ToArray();

        WriteInt32(stub, 0x36, GetRelativeOffset(Add(stubAddress, 0x36), sizeof(int),
            Add(moduleBaseAddress, DialogUiGetGuiBackPaneRva)));
        WriteUInt32(stub, 0x99, checked((uint)Add(moduleBaseAddress, WsprintfImportAddressRva).ToInt64()));
        WriteInt32(stub, 0xCC, DialogItemLabelMaxBytes);
        WriteInt32(stub, 0xF4, GetRelativeOffset(Add(stubAddress, 0xF4), sizeof(int),
            Add(moduleBaseAddress, UiTextTruncateDbcsSafeRva)));
        WriteInt32(stub, 0x146, GetRelativeOffset(Add(stubAddress, 0x146), sizeof(int),
            Add(moduleBaseAddress, DialogItemNameCopyRva)));

        return stub;
    }

    private static byte[] BuildShowItemQuantityInDialogsHook(IntPtr hookAddress, IntPtr stubAddress)
    {
        var hook = new byte[ExpectedItemQuantityDialogHook.Length];
        hook[0] = 0xE8;
        WriteInt32(hook, 1, GetRelativeOffset(hookAddress, hook.Length, stubAddress));
        return hook;
    }

    private static void RestoreItemQuantityDialogHook(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr hookAddress)
    {
        using (allocator.MakeWritable(hookAddress, ExpectedItemQuantityDialogHook.Length))
        {
            writer.BaseStream.Position = hookAddress.ToInt64();
            writer.Write(ExpectedItemQuantityDialogHook);
            allocator.FlushInstructionCache(hookAddress, ExpectedItemQuantityDialogHook.Length);
        }

        VerifyRemoteBytes(writer, hookAddress, ExpectedItemQuantityDialogHook);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
