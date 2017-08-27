namespace DaemonMasterCore.Win32.PInvoke
{
    public static partial class NativeMethods
    {
        /// <summary>
        /// Needed for QueryServiceStatusEx as infoLevel
        /// </summary>
        public const uint SC_STATUS_PROCESS_INFO = 0x0;
        public const int CREATE_NEW_CONSOLE = 0x00000010;

    }
}
