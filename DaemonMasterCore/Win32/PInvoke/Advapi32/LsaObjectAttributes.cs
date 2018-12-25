using System;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32.PInvoke.Advapi32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct LsaObjectAttributes
    {
        public uint Length;
        public IntPtr RootDirectory;
        public IntPtr ObjectName;
        public uint Attributes;
        public IntPtr SecurityDescriptor;
        public IntPtr SecurityQualityOfService;
    }
}
