using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class SecurityAttributes
        {
            public uint length;
            public IntPtr securityDescriptor;
            public bool inheritHandle;
        }
    }
}
