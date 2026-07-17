using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Arbiter.App.Models;
using Arbiter.Interop.Process;
using Arbiter.Interop.Window;

namespace Arbiter.App.Services.Client;

public class GameClientService : IGameClientService
{
    private const string DarkAgesWindowClassName = "Darkages";
    private const long CharacterNameAddress = 0x73d910; // 7.41
    private const int CharacterNameLength = 12;
    
    private const IntPtr MultipleInstancePatchAddress = 0x57A7CE;
    private const IntPtr SkipIntroVideoPatchAddress = 0x42E61F;
    private static readonly (IntPtr Address, byte[] Expected, byte[] Replacement)[] SuppressLoginNoticePatches =
    [
        (0x4B897C, [0x75, 0x6C], [0xEB, 0x6C]),
        (0x4B8ACF, [0x75, 0x6D], [0xEB, 0x6D]),
    ];
    private static readonly IntPtr[] ServerHostnamePatchAddresses = [0x433392, 0x565628];
    private const IntPtr ServerFallbackIpPatchAddress = 0x4333C3;
    private const IntPtr ServerPortPatchAddress = 0x4333E3;

    public async Task<int> LaunchLoopbackClient(string clientExecutablePath, LaunchClientOptions options)
    {
        if (options.LocalPort is < 1 or > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Port must be between 1 and 65535");
        }

        using var process = SuspendedProcess.Start(clientExecutablePath);
        await using var stream = process.GetProcessMemoryStream();
        await using var writer = new BinaryWriter(stream, Encoding.UTF8);

        // Allocate memory to write our hostname instead
        using var allocator = process.GetProcessMemoryAllocator();
        var hostnamePointer = allocator.AllocMemory(mem => { mem.WriteNullTerminated("localhost"); });

        ApplyMultipleInstancePatch(writer);

        if (options.SkipIntroVideo)
        {
            ApplySkipIntroVideoPatch(writer);
        }

        if (options.SuppressLoginNotice)
        {
            ApplySuppressLoginNoticePatch(writer);
        }

        ApplyServerEndpointPatch(writer, hostnamePointer, options.LocalPort);

        return process.ProcessId;
    }

    public IEnumerable<GameClientWindow> GetGameClients()
    {
        var nameBuffer = ArrayPool<byte>.Shared.Rent(CharacterNameLength + 1);
        try
        {
            // Find all client windows by class name
            var windows = NativeWindowEnumerator.FindWindows(DarkAgesWindowClassName);
            foreach (var window in windows)
            {
                // Read the character name from the process memory
                using var stream = ProcessMemoryStream.Open(window.ProcessId, ProcessAccessFlags.Read);
                stream.Position = CharacterNameAddress;
                stream.ReadExactly(nameBuffer);

                // Extract the character name from the buffer, null-terminating it
                var nameBufferText = Encoding.ASCII.GetString(nameBuffer);
                var characterNameLength = nameBufferText.IndexOf('\0');

                yield return new GameClientWindow
                {
                    ProcessId = window.ProcessId,
                    WindowHandle = window.Handle,
                    WindowClassName = window.ClassName,
                    WindowTitle = window.Title,
                    CharacterName = nameBufferText[..characterNameLength],
                };
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(nameBuffer);
        }
    }

    private static void ApplyMultipleInstancePatch(BinaryWriter writer)
    {
        // Overwrite the CreateMutex check with a simple XOR of EAX
        writer.BaseStream.Position = MultipleInstancePatchAddress;
        writer.Write((byte)0x31); // XOR
        writer.Write((byte)0xC0); // EAX, EAX
        writer.Write((byte)0x90); // NOP
        writer.Write((byte)0x90); // NOP
        writer.Write((byte)0x90); // NOP
        writer.Write((byte)0x90); // NOP
    }

    private static void ApplySkipIntroVideoPatch(BinaryWriter writer)
    {
        // Overwrite intro video timer, allowing to skip it
        writer.BaseStream.Position = SkipIntroVideoPatchAddress;
        writer.Write((byte)0x83); // CMP
        writer.Write((byte)0xFA); // EAX
        writer.Write((byte)0x00); // 0
        writer.Write((byte)0x90); // NOP
        writer.Write((byte)0x90); // NOP
        writer.Write((byte)0x90); // NOP
    }

    private static void ApplySuppressLoginNoticePatch(BinaryWriter writer)
    {
        foreach (var patch in SuppressLoginNoticePatches)
        {
            writer.BaseStream.Position = patch.Address;

            var actual = new byte[patch.Expected.Length];
            writer.BaseStream.ReadExactly(actual);
            if (!actual.SequenceEqual(patch.Expected))
            {
                throw new InvalidDataException($"Unexpected client bytes at 0x{patch.Address:X}: " +
                                               $"expected {Convert.ToHexString(patch.Expected)}, " +
                                               $"found {Convert.ToHexString(actual)}.");
            }

            writer.BaseStream.Position = patch.Address;
            writer.Write(patch.Replacement);
        }
    }

    private static void ApplyServerEndpointPatch(BinaryWriter writer, IntPtr hostnamePointer, int port)
    {
        // Replace the hostname lookup with our allocated memory
        // There seems to be a primary and a secondary (backup) that is attempted when the first one fails
        foreach (var address in ServerHostnamePatchAddresses)
        {
            writer.BaseStream.Position = address;
            writer.Write((uint)hostnamePointer);
        }

        // Overwrite the default port (network order)
        writer.BaseStream.Position = ServerPortPatchAddress;
        writer.Write((byte)0xBA); // MOV EDX, imm32
        writer.Write((byte)(port & 0xFF));
        writer.Write((byte)((port >> 8) & 0xFF));
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);

        // Replace the fallback IP address with the localhost address
        // These are reversed due to RTL ordering for args (C ABI)
        writer.BaseStream.Position = ServerFallbackIpPatchAddress;
        foreach (var ipByte in IPAddress.Loopback.GetAddressBytes().Reverse())
        {
            writer.Write((byte)0x6A); // PUSH imm8
            writer.Write(ipByte);
        }
    }
}
