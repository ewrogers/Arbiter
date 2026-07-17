using System.ComponentModel;
using Arbiter.Interop.Win32;

namespace Arbiter.Interop.Process;

public class ProcessMemoryAllocator : IDisposable
{
    private sealed class MemoryProtectionScope : IDisposable
    {
        private readonly IntPtr _processHandle;
        private readonly IntPtr _address;
        private readonly UIntPtr _size;
        private readonly Win32MemoryProtection _originalProtection;
        private bool _isDisposed;

        public MemoryProtectionScope(IntPtr processHandle, IntPtr address, UIntPtr size,
            Win32MemoryProtection originalProtection)
        {
            _processHandle = processHandle;
            _address = address;
            _size = size;
            _originalProtection = originalProtection;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (!NativeMethods.VirtualProtectEx(_processHandle, _address, _size, _originalProtection, out _))
            {
                throw new Win32Exception();
            }

            _isDisposed = true;
        }
    }

    private bool _isDisposed;
    private readonly bool _leaveOpen;
    
    public IntPtr ProcessHandle { get; private set; }
    
    public ProcessMemoryAllocator(IntPtr processHandle, bool leaveOpen = false)
    {
        ProcessHandle = processHandle;
        _leaveOpen = leaveOpen;
    }

    ~ProcessMemoryAllocator()
    {
        Dispose(false);
    }

    public IntPtr AllocMemory(Action<BinaryWriter> initializer, long? minimumSize = null)
    {
        CheckIfDisposed();
        
        using var memoryStream = new MemoryStream();
        if (minimumSize is > 0)
        {
            memoryStream.SetLength(minimumSize.Value);
        }

        using var writer = new BinaryWriter(memoryStream);
        initializer(writer);
        memoryStream.Position = 0;

        var size = memoryStream.Length;

        var memPointer = NativeMethods.VirtualAllocEx(ProcessHandle, IntPtr.Zero, (UIntPtr)size,
            Win32AllocationType.Commit,
            Win32MemoryProtection.ReadWrite);

        if (memPointer == IntPtr.Zero)
        {
            throw new Win32Exception();
        }

        using var processMemoryStream =
            new ProcessMemoryStream(ProcessHandle, ProcessAccessFlags.ReadWrite, leaveOpen: true);
        processMemoryStream.Position = memPointer;
        memoryStream.CopyTo(processMemoryStream, 4096);

        return memPointer;
    }

    public void FreeMemory(IntPtr memPointer)
    {
        CheckIfDisposed();
        
        if (!NativeMethods.VirtualFreeEx(ProcessHandle, memPointer, UIntPtr.Zero, Win32FreeType.Release))
        {
            throw new Win32Exception();
        }
    }

    public void MakeExecutable(IntPtr address, int size)
    {
        CheckIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (!NativeMethods.VirtualProtectEx(ProcessHandle, address, (UIntPtr)size,
                Win32MemoryProtection.ExecuteRead, out _))
        {
            throw new Win32Exception();
        }
    }

    public IDisposable MakeWritable(IntPtr address, int size)
    {
        CheckIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        var nativeSize = (UIntPtr)size;
        if (!NativeMethods.VirtualProtectEx(ProcessHandle, address, nativeSize,
                Win32MemoryProtection.ExecuteReadWrite, out var originalProtection))
        {
            throw new Win32Exception();
        }

        return new MemoryProtectionScope(ProcessHandle, address, nativeSize, originalProtection);
    }

    public void FlushInstructionCache(IntPtr address, int size)
    {
        CheckIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

        if (!NativeMethods.FlushInstructionCache(ProcessHandle, address, (UIntPtr)size))
        {
            throw new Win32Exception();
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool isDisposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (isDisposing)
        {
            // Cleanup managed resources here
        }

        if (!_leaveOpen && ProcessHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(ProcessHandle);
        }

        ProcessHandle = IntPtr.Zero;
        _isDisposed = true;
    }

    private void CheckIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
}
