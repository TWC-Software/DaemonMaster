namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ServiceCurrentState : uint
        {
            Stopped = 0x00000001,
            StartPending = 0x00000002,
            StopPending = 0x00000003,
            Running = 0x00000004,
            ContinuePending = 0x00000005,
            PausePending = 0x00000006,
            Paused = 0x00000007,
        }
    }
}
