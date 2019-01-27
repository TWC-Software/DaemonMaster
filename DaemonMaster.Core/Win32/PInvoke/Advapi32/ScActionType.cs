namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ScActionType
        {
            None = 0,
            Reboot = 2,
            Restart = 1,
            RunCommand = 3
        }
    }
}
