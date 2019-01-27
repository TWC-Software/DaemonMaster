using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceConfigFailureActionsFlag
        {
            public bool failureActionsOnNonCrashFailures;
        }
    }
}
