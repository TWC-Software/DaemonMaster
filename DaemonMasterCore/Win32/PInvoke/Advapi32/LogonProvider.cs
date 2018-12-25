namespace DaemonMasterCore.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum LogonProvider : uint
        {
            Default = 0x00000000,
            WinNt40 = 0x00000002,
            WinNt50 = 0x00000003
        }
    }
}
