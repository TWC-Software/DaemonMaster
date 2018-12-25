using System;

namespace DaemonMasterCore.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        [Flags]
        public enum ServiceType : uint
        {
            KernelDriver = 0x00000001,
            FileSystemDriver = 0x00000002,
            Adapter = 0x00000004,
            RecognizerDriver = 0x00000008,
            Win32OwnProcess = 0x00000010,
            Win32ShareProcess = 0x00000020,
            UserOwnProcess = 0x00000050,
            UserShareProcess = 0x00000060,

            /// <summary>
            /// Only use it when the service type is either "Win32OwnProcess" or "Win32ShareProcess" and the service run as LocalSystem.
            /// </summary>
            InteractivProcess = 0x00000100
        }
    }
}
