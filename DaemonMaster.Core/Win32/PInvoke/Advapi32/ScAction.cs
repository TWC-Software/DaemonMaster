using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ScAction
        {
            /// <summary>
            /// The action to be performed.
            /// </summary>
            public ScActionType actionType;

            /// <summary>
            /// The time to wait before starting the specified action (in ms).
            /// </summary>
            public TimeSpan delay;
        }
    }
}
