using System;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [Flags]
        public enum ThreadAccess : uint
        {
            Terminate = 0x0001,
            SuspendResume = 0x0002,
            GetContext = 0x0008,
            SetContext = 0x0010,
            SetInformation = 0x0020,
            QueryInformation = 0x0040,
            SetThreadToken = 0x0080,
            Impersonate = 0x0100,
            DirectImpersonation = 0x0200
        }
    }
}
