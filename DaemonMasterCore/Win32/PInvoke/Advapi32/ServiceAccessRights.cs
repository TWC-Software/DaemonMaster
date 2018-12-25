using System;

namespace DaemonMasterCore.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [Flags]
        public enum ServiceAccessRights : uint
        {
            AllAccess = 0xF01FF,
            ChangeConfig = 0x0002,
            EnumerateDependents = 0x0008,
            Interrogate = 0x0080,
            PauseContinue = 0x0040,
            QueryConfig = 0x0001,
            QueryStatus = 0x0004,
            Start = 0x0010,
            Stop = 0x0020,
            UserDefinedControl = 0x0100,
        }
    }
}
