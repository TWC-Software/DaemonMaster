using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public IntPtr ProcessMemoryLimit;
            public IntPtr JobMemoryLimit;
            public IntPtr PeakProcessMemoryUsed;
            public IntPtr PeakJobMemoryUsed;
        }
    }
}
