using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterCore.Win32.PInvoke
{
    public partial class NativeMethods
    {
        [DllImport(DLLFiles.WTSAPI32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WTSQueryUserToken(UInt32 sessionId, out TokenHandle Token);
    }
}
