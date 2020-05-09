using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct StartupInfo
        {
            public uint cb;
            public string reserved1LPSTR;
            public string desktop;
            public string title;
            public uint X;
            public uint Y;
            public uint XSize;
            public uint YSize;
            public uint XCountChars;
            public uint YCountChars;
            public uint fillAttribute;
            public uint flags;
            public ushort showWindow;
            public ushort reserved2USHORT;
            public IntPtr reserved1LPBYTE;
            public IntPtr stdInputHandle;
            public IntPtr stdOutputHandle;
            public IntPtr stdErrorHandle;
        }
    }
}
