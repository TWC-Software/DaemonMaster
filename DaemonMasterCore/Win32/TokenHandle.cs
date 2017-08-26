using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32
{
    class TokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public TokenHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }

        public static TokenHandle GetTokenFromSessionID(uint sessionID)
        {
            TokenHandle currentUserToken;
            if (!NativeMethods.WTSQueryUserToken(sessionID, out currentUserToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return currentUserToken;
        }
    }
}
