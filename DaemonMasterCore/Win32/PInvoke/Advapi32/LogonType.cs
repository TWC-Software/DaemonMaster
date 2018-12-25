namespace DaemonMasterCore.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum LogonType : uint
        {
            Interactive = 0x00000002,
            Network = 0x00000003,
            Batch = 0x00000004,
            Service = 0x00000005,
            Unlock = 0x00000007,
            NetworkClearText = 0x00000008,
            NewCredentials = 0x00000009
        }
    }
}
