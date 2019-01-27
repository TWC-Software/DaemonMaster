using System;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        /// <summary>
        ///  Specifies the access rights for the "Service Control Manager".
        /// </summary>
        [Flags]
        public enum ServiceControlManagerAccessRights : uint
        {
            Connect = 0x0001,
            CreateService = 0x0002,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            QueryLockStatus = 0x0010,
            ModifyBootConfig = 0x0020,
            AllAccess = 0xF003F,
        }
    }
}
