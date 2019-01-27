namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ErrorControl : uint
        {
            /// <summary>
            /// MSDN help:
            /// The startup program ignores the error and continues the startup operation.
            /// </summary>
            Ignore = 0x00000000,

            /// <summary>
            /// MSDN help:
            /// The startup program logs the error in the event log but continues the startup operation.
            /// </summary>
            Normal = 0x00000001,

            /// <summary>
            /// MSDN help:
            /// The startup program logs the error in the event log. If the last-known-good configuration is being started, the startup operation continues. Otherwise, the system is restarted with the last-known-good configuration.
            /// </summary>
            Severe = 0x00000002,

            /// <summary>
            /// MSDN help:
            /// The startup program logs the error in the event log, if possible. If the last-known-good configuration is being started, the startup operation fails. Otherwise, the system is restarted with the last-known good configuration.
            /// </summary>
            Critical = 0x00000003,
        }
    }
}
