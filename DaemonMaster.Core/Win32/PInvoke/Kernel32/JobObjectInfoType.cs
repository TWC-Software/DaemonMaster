namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        public enum JobObjectInfoType : uint
        {
            BasicLimitInformation = 2,
            BasicUiRestrictions = 4,
            SecurityLimitInformation = 5,
            EndOfJobTimeInformation = 6,
            AssociateCompletionPortInformation = 7,
            ExtendedLimitInformation = 9,
            GroupInformation = 11
        }
    }
}
