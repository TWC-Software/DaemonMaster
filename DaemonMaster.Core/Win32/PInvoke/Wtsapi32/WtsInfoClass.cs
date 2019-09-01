namespace DaemonMaster.Core.Win32.PInvoke.Wtsapi32
{
    public partial class Wtsapi32
    {
        public enum WtsInfoClass
        {
            WtsInitialProgram,
            WtsApplicationName,
            WtsWorkingDirectory,
            WtsOemId,
            WtsSessionId,
            WtsUserName,
            WtsWinStationName,
            WtsDomainName,
            WtsConnectState,
            WtsClientBuildNumber,
            WtsClientName,
            WtsClientDirectory,
            WtsClientProductId,
            WtsClientHardwareId,
            WtsClientAddress,
            WtsClientDisplay,
            WtsClientProtocolType,
            WtsIdleTime,
            WtsLogonTime,
            WtsIncomingBytes,
            WtsOutgoingBytes,
            WtsIncomingFrames,
            WtsOutgoingFrames,
            WtsClientInfo,
            WtsSessionInfo
        }
    }
}
