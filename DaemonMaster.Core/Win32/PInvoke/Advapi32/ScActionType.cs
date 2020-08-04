namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ScActionType
        {
            None = 0,
            Restart = 1,
            Reboot = 2,
            RunCommand = 3
        }
    }
}
