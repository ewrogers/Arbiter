using System.Runtime.InteropServices;
using System.Text;

namespace Arbiter.Interop.Win32;

[return: MarshalAs(UnmanagedType.Bool)]
internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

internal static class NativeMethods
{
    [DllImport("user32.dll", EntryPoint = "EnumWindows", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnumWindows(EnumWindowsProc enumWindowProc, IntPtr lParam);
    
    [DllImport("user32", EntryPoint = "GetWindowThreadProcessId", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern int GetWindowThreadProcessId(IntPtr windowHandle, out int processId);
    
    [DllImport("user32", EntryPoint = "GetClassName", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetClassName(IntPtr windowHandle, StringBuilder className, int maxLength);
    
    [DllImport("user32", EntryPoint = "GetWindowTextLength", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern int GetWindowTextLength(IntPtr windowHandle);

    [DllImport("user32", EntryPoint = "GetWindowText", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern int GetWindowText(IntPtr windowHandle, StringBuilder windowText, int maxLength);
    
    [DllImport("user32", EntryPoint = "SetWindowText", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowText(IntPtr windowHandle, string windowText);
    
    [DllImport("user32", EntryPoint = "SetForegroundWindow", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr windowHandle);
    
    [DllImport("kernel32.dll", EntryPoint = "CreateProcess", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcess(string applicationName,
        string? commandLine,
        ref Win32SecurityAttributes processAttributes,
        ref Win32SecurityAttributes threadAttributes,
        bool inheritHandles,
        Win32ProcessCreationFlags creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref Win32StartupInfo startupInfo,
        out Win32ProcessInformation processInformation);

    [DllImport("kernel32", EntryPoint = "OpenProcess", CharSet = CharSet.Auto, SetLastError = true)]
    internal static extern IntPtr OpenProcess(Win32ProcessAccessFlags desiredAccess, bool inheritHandle, int processId);
    
    [DllImport("kernel32.dll", EntryPoint = "ResumeThread", SetLastError = true)]
    internal static extern uint ResumeThread(IntPtr hThread);

    [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess")]
    internal static extern int NtQueryInformationProcess(IntPtr processHandle,
        Win32ProcessInformationClass processInformationClass, out IntPtr processInformation,
        int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll", EntryPoint = "NtQueryInformationProcess")]
    internal static extern int NtQueryInformationProcess(IntPtr processHandle,
        Win32ProcessInformationClass processInformationClass, out Win32ProcessBasicInformation processInformation,
        int processInformationLength, out int returnLength);

    [DllImport("ntdll.dll", EntryPoint = "RtlNtStatusToDosError")]
    internal static extern uint RtlNtStatusToDosError(int status);

    [DllImport("kernel32.dll", EntryPoint = "ReadProcessMemory", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr baseAddress, byte[] buffer, int size,
        out IntPtr bytesRead);

    [DllImport("kernel32.dll", EntryPoint = "WriteProcessMemory", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr baseAddress, byte[] buffer, int size,
        out IntPtr bytesWritten);

    [DllImport("kernel32.dll", EntryPoint = "VirtualAllocEx", SetLastError = true)]
    internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr baseAddress, UIntPtr desiredSize,
        Win32AllocationType allocationType, Win32MemoryProtection protect);

    [DllImport("kernel32.dll", EntryPoint = "VirtualProtectEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr baseAddress, UIntPtr size,
        Win32MemoryProtection newProtect, out Win32MemoryProtection oldProtect);

    [DllImport("kernel32.dll", EntryPoint = "VirtualFreeEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr baseAddress, UIntPtr size,
        Win32FreeType freeType);

    [DllImport("kernel32.dll", EntryPoint = "FlushInstructionCache", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr baseAddress, UIntPtr size);

    [DllImport("kernel32.dll", EntryPoint = "TerminateProcess", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateProcess(IntPtr hProcess, uint exitCode);

    [DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
    internal static extern IntPtr OpenProcess(Win32ProcessAccess desireAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr")]
    internal static extern IntPtr GetClassLongPtr(IntPtr hWindow, Win32GetClassLongIndex index);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtr")]
    internal static extern IntPtr SetClassLongPtr(IntPtr hWindow, Win32GetClassLongIndex index, IntPtr newValue);

    [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(IntPtr hWindow, Win32ShowWindowCommand showCommand);
}
