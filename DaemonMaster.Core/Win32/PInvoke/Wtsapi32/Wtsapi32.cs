using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Wtsapi32
{
    public static class Wtsapi32
    {
        private const string DllName = "wtsapi32.dll";

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(uint sessionId, out TokenHandle tokenHandle);
    }
}
