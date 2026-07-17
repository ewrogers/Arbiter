using System.ComponentModel;
using System.Runtime.InteropServices;
using Arbiter.Interop.Win32;

namespace Arbiter.Interop.Process;

public class SuspendedProcess : IDisposable
{
    private bool _isDisposed;

    public IntPtr ProcessHandle { get; private set; }
    public IntPtr ThreadHandle { get; private set; }
    public int ProcessId { get; private set; }
    public int ThreadId { get; private set; }
    public bool IsSuspended { get; private set; }

    private SuspendedProcess(IntPtr processHandle, IntPtr threadHandle, int processId, int threadId)
    {
        ProcessHandle = processHandle;
        ThreadHandle = threadHandle;
        ProcessId = processId;
        ThreadId = threadId;

        IsSuspended = true;
    }

    ~SuspendedProcess()
    {
        Dispose(false);
    }

    public static SuspendedProcess Start(string filename, IEnumerable<string>? arguments = null)
    {
        var commandLine = arguments is not null ? string.Join(' ', arguments) : null;

        var startupInfo = new Win32StartupInfo
        {
            Size = Marshal.SizeOf<Win32StartupInfo>(),
        };

        var securityAttributes = new Win32SecurityAttributes
        {
            Size = Marshal.SizeOf<Win32SecurityAttributes>(),
            SecurityDescriptor = IntPtr.Zero,
            InheritHandle = false
        };

        var success = NativeMethods.CreateProcess(filename, commandLine,
            ref securityAttributes,
            ref securityAttributes,
            false,
            Win32ProcessCreationFlags.Suspended,
            IntPtr.Zero,
            null,
            ref startupInfo,
            out var processInformation);

        if (!success || processInformation.ProcessId == 0)
        {
            throw new Win32Exception();
        }

        return new SuspendedProcess(processInformation.ProcessHandle, processInformation.ThreadHandle,
            processInformation.ProcessId, processInformation.ThreadId);
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

        if (IsSuspended)
        {
            Resume();
        }

        if (ProcessHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(ProcessHandle);
        }

        if (ThreadHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(ThreadHandle);
        }

        ProcessHandle = IntPtr.Zero;
        ThreadHandle = IntPtr.Zero;

        _isDisposed = true;
    }
    
    public ProcessMemoryStream GetProcessMemoryStream(ProcessAccessFlags accessFlags = ProcessAccessFlags.ReadWrite)
    {
        CheckIfDisposed();
        
        var desiredAccessFlags = Win32ProcessAccess.QueryInformation | Win32ProcessAccess.VmOperation;
        if (accessFlags.HasFlag(ProcessAccessFlags.Read))
        {
            desiredAccessFlags |= Win32ProcessAccess.VmRead;
        }

        if (accessFlags.HasFlag(ProcessAccessFlags.Write))
        {
            desiredAccessFlags |= Win32ProcessAccess.VmWrite;
        }

        var newHandle = NativeMethods.OpenProcess(desiredAccessFlags, false, ProcessId);
        if (newHandle == IntPtr.Zero)
        {
            throw new Win32Exception();
        }

        return new ProcessMemoryStream(newHandle, accessFlags);
    }

    public ProcessMemoryAllocator GetProcessMemoryAllocator()
    {
        CheckIfDisposed();
        
        const Win32ProcessAccess readWriteAccess = Win32ProcessAccess.QueryInformation |
                                                   Win32ProcessAccess.VmOperation |
                                                   Win32ProcessAccess.VmRead | Win32ProcessAccess.VmWrite;

        var newHandle = NativeMethods.OpenProcess(readWriteAccess, false, ProcessId);
        if (newHandle == IntPtr.Zero)
        {
            throw new Win32Exception();
        }

        return new ProcessMemoryAllocator(newHandle);
    }

    public IntPtr GetImageBaseAddress()
    {
        CheckIfDisposed();

        IntPtr pebAddress;
        if (Environment.Is64BitProcess)
        {
            var status = NativeMethods.NtQueryInformationProcess(ProcessHandle,
                Win32ProcessInformationClass.Wow64Information, out pebAddress, IntPtr.Size, out _);
            ThrowIfNtStatusFailed(status);

            if (pebAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException("The suspended client is not a 32-bit process.");
            }
        }
        else
        {
            var status = NativeMethods.NtQueryInformationProcess(ProcessHandle,
                Win32ProcessInformationClass.BasicInformation, out Win32ProcessBasicInformation processInformation,
                Marshal.SizeOf<Win32ProcessBasicInformation>(), out _);
            ThrowIfNtStatusFailed(status);
            pebAddress = processInformation.PebBaseAddress;
        }

        var imageBaseBytes = new byte[sizeof(uint)];
        var imageBasePointerAddress = IntPtr.Add(pebAddress, 0x08);
        if (!NativeMethods.ReadProcessMemory(ProcessHandle, imageBasePointerAddress, imageBaseBytes,
                imageBaseBytes.Length, out var bytesRead) || bytesRead.ToInt64() != imageBaseBytes.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                "Could not read the client image base from the suspended process.");
        }

        var imageBaseAddress = new IntPtr(BitConverter.ToUInt32(imageBaseBytes));
        return imageBaseAddress != IntPtr.Zero
            ? imageBaseAddress
            : throw new InvalidOperationException("The suspended client image base is null.");
    }

    public void Resume()
    {
        CheckIfDisposed();

        while (ThreadHandle != IntPtr.Zero && NativeMethods.ResumeThread(ThreadHandle) > 1)
        {
            // Keep resuming until completely unsuspended
        }

        IsSuspended = false;
    }

    public void Kill(int exitCode = 0)
    {
        CheckIfDisposed();

        if (ProcessHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Process is not running");
        }

        if (!NativeMethods.TerminateProcess(ProcessHandle, (uint)exitCode))
        {
            throw new Win32Exception();
        }

        ProcessHandle = IntPtr.Zero;
        ThreadHandle = IntPtr.Zero;
        ProcessId = 0;
        ThreadId = 0;
        IsSuspended = false;
    }

    private static void ThrowIfNtStatusFailed(int status)
    {
        if (status >= 0)
        {
            return;
        }

        var error = NativeMethods.RtlNtStatusToDosError(status);
        throw new Win32Exception(checked((int)error));
    }

    private void CheckIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
}
