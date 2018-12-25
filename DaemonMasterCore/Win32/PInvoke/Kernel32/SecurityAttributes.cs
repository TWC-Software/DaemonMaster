using System;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SecurityAttributes
        {
            public uint length;
            public IntPtr securityDescriptor;
            public bool inheritHandle;
        }
    }
}
