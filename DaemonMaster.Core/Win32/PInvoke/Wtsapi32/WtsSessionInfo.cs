using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Wtsapi32
{
    public partial class Wtsapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct WtsSessionInfo
        {
            public Int32 SessionID;
            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;
            public WtsConnectstateClass State;
        }
    }
}
