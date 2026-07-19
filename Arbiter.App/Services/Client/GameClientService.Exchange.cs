using System;
using System.IO;
using System.Linq;
using Arbiter.Interop.Process;

namespace Arbiter.App.Services.Client;

public partial class GameClientService
{
    private const int ExchangeQuantityPromptHookRva = 0x0006A690;
    private const int ExchangeQuantityPromptContinuationRva = 0x0006A695;
    private const int UiGetGuiBackPaneRva = 0x001A9C40;
    private const int ExchangeCountBuilderRva = 0x0006C2A0;

    private static readonly byte[] ExpectedExchangeQuantityPromptHook = [0x55, 0x8B, 0xEC, 0x6A, 0xFF];
    private static readonly byte[] SkipExchangeQuantityPromptStubTemplate =
    [
        0x53, 0x56, 0x57, 0x89, 0xCE, 0x8B, 0x7C, 0x24, 0x10, 0x0F, 0xB6, 0x5F, 0x02, 0x84, 0xDB, 0x74,
        0x5F, 0x80, 0xFB, 0x3C, 0x77, 0x5A, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x85, 0xC0, 0x74, 0x51, 0x8B,
        0x80, 0x88, 0x4F, 0x00, 0x00, 0x85, 0xC0, 0x74, 0x47, 0x0F, 0xB6, 0xCB, 0x49, 0x8B, 0x84, 0x88,
        0xA0, 0x01, 0x00, 0x00, 0x85, 0xC0, 0x74, 0x38, 0x80, 0xB8, 0x44, 0x02, 0x00, 0x00, 0x00, 0x74,
        0x2F, 0x83, 0xB8, 0x40, 0x02, 0x00, 0x00, 0x01, 0x75, 0x26, 0x0F, 0xB6, 0x86, 0x34, 0x06, 0x00,
        0x00, 0x50, 0x88, 0x9E, 0x34, 0x06, 0x00, 0x00, 0x6A, 0x01, 0x89, 0xF1, 0xE8, 0x00, 0x00, 0x00,
        0x00, 0x5A, 0x88, 0x96, 0x34, 0x06, 0x00, 0x00, 0x5F, 0x5E, 0x5B, 0x31, 0xC0, 0xC2, 0x04, 0x00,
        0x89, 0xF1, 0x5F, 0x5E, 0x5B, 0x55, 0x89, 0xE5, 0x6A, 0xFF, 0xE9, 0x00, 0x00, 0x00, 0x00,
    ];

    private static void ApplySkipExchangeQuantityPromptPatch(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr moduleBaseAddress)
    {
        var stage = "verify the exchange quantity-prompt hook";
        var stubAddress = IntPtr.Zero;
        var hookWriteStarted = false;

        try
        {
            var hookAddress = Add(moduleBaseAddress, ExchangeQuantityPromptHookRva);
            var actualHook = ReadRemoteBytes(writer, hookAddress, ExpectedExchangeQuantityPromptHook.Length);
            if (!actualHook.SequenceEqual(ExpectedExchangeQuantityPromptHook))
            {
                throw new InvalidDataException($"Unexpected client bytes at 0x{hookAddress.ToInt64():X}: " +
                                               $"expected {Convert.ToHexString(ExpectedExchangeQuantityPromptHook)}, " +
                                               $"found {Convert.ToHexString(actualHook)}.");
            }

            stage = "allocate the exchange quantity-prompt stub";
            stubAddress = allocator.AllocMemory(_ => { }, SkipExchangeQuantityPromptStubTemplate.Length);
            var stub = BuildSkipExchangeQuantityPromptStub(moduleBaseAddress, stubAddress);

            stage = "write the exchange quantity-prompt stub";
            writer.BaseStream.Position = stubAddress.ToInt64();
            writer.Write(stub);

            stage = "protect and verify the exchange quantity-prompt stub";
            allocator.MakeExecutable(stubAddress, stub.Length);
            VerifyRemoteBytes(writer, stubAddress, stub);
            allocator.FlushInstructionCache(stubAddress, stub.Length);

            var replacementHook = BuildSkipExchangeQuantityPromptHook(hookAddress, stubAddress);

            stage = "write the exchange quantity-prompt hook";
            using (allocator.MakeWritable(hookAddress, replacementHook.Length))
            {
                hookWriteStarted = true;
                writer.BaseStream.Position = hookAddress.ToInt64();
                writer.Write(replacementHook);
                allocator.FlushInstructionCache(hookAddress, replacementHook.Length);
            }

            stage = "verify the exchange quantity-prompt hook";
            VerifyRemoteBytes(writer, hookAddress, replacementHook);
        }
        catch (Exception exception)
        {
            Exception? cleanupException = null;
            var hookRestored = !hookWriteStarted;
            var hookAddress = Add(moduleBaseAddress, ExchangeQuantityPromptHookRva);

            if (hookWriteStarted)
            {
                try
                {
                    RestoreExchangeQuantityPromptHook(writer, allocator, hookAddress);
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

    private static byte[] BuildSkipExchangeQuantityPromptStub(IntPtr moduleBaseAddress, IntPtr stubAddress)
    {
        var stub = SkipExchangeQuantityPromptStubTemplate.ToArray();

        WriteInt32(stub, 0x17, GetRelativeOffset(Add(stubAddress, 0x17), sizeof(int),
            Add(moduleBaseAddress, UiGetGuiBackPaneRva)));
        WriteInt32(stub, 0x5D, GetRelativeOffset(Add(stubAddress, 0x5D), sizeof(int),
            Add(moduleBaseAddress, ExchangeCountBuilderRva)));
        WriteInt32(stub, 0x7B, GetRelativeOffset(Add(stubAddress, 0x7B), sizeof(int),
            Add(moduleBaseAddress, ExchangeQuantityPromptContinuationRva)));

        return stub;
    }

    private static byte[] BuildSkipExchangeQuantityPromptHook(IntPtr hookAddress, IntPtr stubAddress)
    {
        var hook = new byte[ExpectedExchangeQuantityPromptHook.Length];
        hook[0] = 0xE9;
        WriteInt32(hook, 1, GetRelativeOffset(hookAddress, hook.Length, stubAddress));
        return hook;
    }

    private static void RestoreExchangeQuantityPromptHook(BinaryWriter writer, ProcessMemoryAllocator allocator,
        IntPtr hookAddress)
    {
        using (allocator.MakeWritable(hookAddress, ExpectedExchangeQuantityPromptHook.Length))
        {
            writer.BaseStream.Position = hookAddress.ToInt64();
            writer.Write(ExpectedExchangeQuantityPromptHook);
            allocator.FlushInstructionCache(hookAddress, ExpectedExchangeQuantityPromptHook.Length);
        }

        VerifyRemoteBytes(writer, hookAddress, ExpectedExchangeQuantityPromptHook);
    }
}
