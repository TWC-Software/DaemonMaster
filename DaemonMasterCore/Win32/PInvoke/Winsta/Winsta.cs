using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32.PInvoke.Winsta
{
    public static class Winsta
    {
        private const string DllName = "winsta.dll";

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        public static extern void WinStationSwitchToServicesSession();
    }
}
