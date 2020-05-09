using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ProcessInformation
        {
            public IntPtr processHandle;
            public IntPtr threadHandle;
            public uint processId;
            public uint threadId;
        }
    }
}
