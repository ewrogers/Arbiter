using System.Runtime.InteropServices;

namespace Arbiter.Interop.Win32;

[StructLayout(LayoutKind.Sequential)]
internal struct Win32ProcessBasicInformation
{
    public int ExitStatus;
    public IntPtr PebBaseAddress;
    public UIntPtr AffinityMask;
    public int BasePriority;
    public UIntPtr UniqueProcessId;
    public UIntPtr InheritedFromUniqueProcessId;
}
