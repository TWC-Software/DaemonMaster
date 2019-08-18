namespace DaemonMaster.Core.Win32.PInvoke.Wtsapi32
{
    public partial class Wtsapi32
    {
        public enum WtsConnectstateClass
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }
    }
}
