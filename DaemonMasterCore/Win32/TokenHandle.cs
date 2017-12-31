using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32
{
    public class TokenHandle : SafeHandleZeroOrMinusOneIsInvalid
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

        public static TokenHandle GetTokenFromLogon(string username, SecureString password,
            NativeMethods.LOGON_TYP logonTyp)
        {
            TokenHandle token;
            IntPtr passwordHandle = IntPtr.Zero;

            try
            {
                passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);
                if (!NativeMethods.LogonUser(username, ".", passwordHandle, logonTyp, NativeMethods.LOGON_PROVIDER.Default, out token))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return token;
            }
            finally
            {
                if (passwordHandle != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordHandle);
            }
        }
    }
}
