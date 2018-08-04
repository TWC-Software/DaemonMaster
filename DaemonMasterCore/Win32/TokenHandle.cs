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

        public static TokenHandle GetTokenFromLogon(string username, SecureString password, NativeMethods.LOGON_TYP logonTyp)
        {
            IntPtr passwordHandle = IntPtr.Zero;
            try
            {
                passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);
                if (!NativeMethods.LogonUser(GetLogin(username), GetDomain(username), passwordHandle, logonTyp, NativeMethods.LOGON_PROVIDER.Default, out var token))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return token;
            }
            finally
            {
                if (passwordHandle != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordHandle);
            }
        }

        private static string GetDomain(string s)
        {
            int stop = s.IndexOf("\\", StringComparison.Ordinal);
            return (stop > -1) ? s.Substring(0, stop) : string.Empty;
        }

        private static string GetLogin(string s)
        {
            int stop = s.IndexOf("\\", StringComparison.Ordinal);
            return (stop > -1) ? s.Substring(stop + 1, s.Length - stop - 1) : string.Empty;
        }
    }
}
