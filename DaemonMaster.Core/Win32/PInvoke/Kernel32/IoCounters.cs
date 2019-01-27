using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }
    }
}
