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

        public static TokenHandle GetTokenFromSessionId(int sessionId)
        {
            if (!NativeMethods.WTSQueryUserToken(sessionId, out var currentUserToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return currentUserToken;
        }

        /// <summary>
        /// Logon and return the token
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="logonTyp"></param>
        /// <returns></returns>
        public static TokenHandle GetTokenFromLogon(string username, SecureString password, NativeMethods.LOGON_TYP logonTyp)
        {
            IntPtr passwordHandle = IntPtr.Zero;
            try
            {
                passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);
                if (!NativeMethods.LogonUser(DaemonMasterUtils.GetLoginFromUsername(username), DaemonMasterUtils.GetDomainFromUsername(username), passwordHandle, logonTyp, NativeMethods.LOGON_PROVIDER.Default, out var token))
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
