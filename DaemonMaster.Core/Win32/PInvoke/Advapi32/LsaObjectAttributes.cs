using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LsaObjectAttributes
    {
        public int Length;
        public IntPtr RootDirectory;
        public Advapi32.LsaUnicodeString ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }
}
