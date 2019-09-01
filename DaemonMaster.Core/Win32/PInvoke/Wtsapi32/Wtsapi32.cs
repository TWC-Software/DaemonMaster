using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Wtsapi32
{
    public static partial class Wtsapi32
    {
        public const uint InvalidSessionId = 0xFFFFFFFF;
        public static readonly IntPtr WtsCurrentServerHandle = IntPtr.Zero;

        public const string DllName = "wtsapi32.dll";

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(uint sessionId, out TokenHandle tokenHandle);


        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSEnumerateSessions
        (
            IntPtr server,
            int reserved,
            int version,
            ref IntPtr sessionInfo,
            ref int count
        );

        [DllImport(DllName, ExactSpelling = true)]
        public static extern void WTSFreeMemory(IntPtr sessionInfo);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WTSQuerySessionInformation(IntPtr server, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr buffer, out uint bytesReturned);
    }
}
