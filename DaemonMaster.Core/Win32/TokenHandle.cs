using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Core.Win32.PInvoke.Kernel32;
using DaemonMaster.Core.Win32.PInvoke.Wtsapi32;
using Microsoft.Win32.SafeHandles;

namespace DaemonMaster.Core.Win32
{
    public class TokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public TokenHandle() : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Kernel32.CloseHandle(handle);
        }

        /// <summary>
        /// Gets the logon token from a session ID. Only possible if the caller has LocalSystem rights
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns></returns>
        public static TokenHandle GetTokenFromSessionId(uint sessionId)
        {
            if (!Wtsapi32.WTSQueryUserToken(sessionId, out TokenHandle currentUserToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return currentUserToken;
        }

        /// <summary>
        /// Allows you to get the token from a user logon
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password to login</param>
        /// <param name="logonTyp">The logon type</param>
        /// <returns></returns>
        public static TokenHandle GetTokenFromLogon(string username, SecureString password, Advapi32.LogonType logonTyp)
        {
            IntPtr passwordHandle = IntPtr.Zero;

            try
            {
                passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);
                if (!Advapi32.LogonUser(DaemonMasterUtils.GetLoginFromUsername(username), DaemonMasterUtils.GetDomainFromUsername(username), passwordHandle, logonTyp, Advapi32.LogonProvider.Default, out TokenHandle token))
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
