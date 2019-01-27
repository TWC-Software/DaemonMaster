namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ServiceInfoLevel : uint
        {
            Description = 1,
            FailureActions = 2,
            DelayedAutoStartInfo = 3,
            FailureActionsFlag = 4,
            ServiceSidInfo = 5,
            PrivilegesInfo = 6,
            PreshutdownInfo = 7,
            TriggerInfo = 8,
            PreferredNode = 9,
            LaunchProtected = 12
        }
    }
}
