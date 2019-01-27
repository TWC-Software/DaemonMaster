namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ServiceControl : uint
        {
            Stop = 0x00000001,
            Pause = 0x00000002,
            Continue = 0x00000003,
            Interrogate = 0x00000004,
            ParamChange = 0x00000006,
            NetBindAdd = 0x00000007,
            NetBindRemove = 0x00000008,
            NetBindEnable = 0x00000009,
            NetbindDisable = 0x0000000A,
        }
    }
}
