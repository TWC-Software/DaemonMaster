using System;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ServiceConfigFailureActions
        {
            /// <summary>
            ///  Time that is necessary after the last failure, to restet the failure count.
            /// </summary>
            public TimeSpan resetPeriode;

            /// <summary>
            /// The reboot message (only when a reboot action failure is configured)
            /// </summary>
            public string rebootMessage;

            /// <summary>
            /// The command line of a process that excecute as response to an "run command" action.
            /// </summary>
            public string command;

            /// <summary>
            /// Length of the actions array.
            /// When this value is 0, but actions is not null, the reset period and array of failure actions are deleted.
            /// </summary>
            public uint actionsLength;

            /// <summary>
            /// Array of actions.
            /// When this value is null, the actionsLength and resetPeriode members are ignored.
            /// </summary>
            [MarshalAs(UnmanagedType.LPArray)]
            public ScAction[] actions;
        }
    }
}
