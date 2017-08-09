using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DaemonMasterCore.Win32.PInvoke;

namespace DaemonMasterCore.Win32
{
    class TokenHandle : SafeHandle
    {
        public TokenHandle() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static TokenHandle GetTokenFromSessionID(uint sessionID)
        {
            TokenHandle currentUserToken;
            if (!NativeMethods.WTSQueryUserToken(sessionID, out currentUserToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return currentUserToken;
        }
    }
}
