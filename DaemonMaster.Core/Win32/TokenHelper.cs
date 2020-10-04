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
    public static class TokenHelper
    {
        /// <summary>
        /// Allows you to get the user token from a user logon
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password to login</param>
        /// <param name="logonTyp">The logon type</param>
        /// <returns>A <see cref="TokenHelper"/> or null if no valid sessions was found.</returns>
        public static SafeAccessTokenHandle GetUserTokenFromLogon(string username, SecureString password, Advapi32.LogonType logonTyp)
        {
            IntPtr passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);
            try
            {
                if (!Advapi32.LogonUser(DaemonMasterUtils.GetLoginFromUsername(username), DaemonMasterUtils.GetDomainFromUsername(username), passwordHandle, logonTyp, Advapi32.LogonProvider.Default, out SafeAccessTokenHandle token))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return token;
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(passwordHandle);
            }
        }

        /// <summary>
        /// Gets the primary token from a session ID. Only possible if the caller has LocalSystem rights
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <returns></returns>
        public static SafeAccessTokenHandle GetPrimaryTokenFromSessionId(uint sessionId)
        {
            if (!Wtsapi32.WTSQueryUserToken(sessionId, out SafeAccessTokenHandle currentUserToken))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return currentUserToken;
        }

        /// <summary>
        /// Gets the primary token of the first found active user session.
        /// </summary>
        /// <returns>A <see cref="TokenHelper"/> or null if no valid sessions was found.</returns>
        /// <exception cref="Win32Exception"></exception>
        public static SafeAccessTokenHandle GetPrimaryTokenOfFirstActiveSession()
        {
            //!!! Parts are from Copyright (c) 2014 Justin Murray - MIT-License, thanks to him !!!
            // https://github.com/murrayju/CreateProcessAsUser, 18.08.2019

            uint activeSessionId = Wtsapi32.InvalidSessionId;
            IntPtr sessionInfos = IntPtr.Zero;
            var sessionCount = 0;

            try
            {
                //Get a handle to the user access token for the current active session.
                if (Wtsapi32.WTSEnumerateSessions(Wtsapi32.WtsCurrentServerHandle, 0, 1, ref sessionInfos, ref sessionCount))
                {
                    int arrayElementSize = Marshal.SizeOf<Wtsapi32.WtsSessionInfo>();
                    IntPtr current = sessionInfos;

                    for (var i = 0; i < sessionCount; i++)
                    {
                        var sessionInfo = Marshal.PtrToStructure<Wtsapi32.WtsSessionInfo>(current);
                        current += arrayElementSize;

                        if (sessionInfo.State == Wtsapi32.WtsConnectstateClass.WTSActive)
                        {
                            activeSessionId = (uint)sessionInfo.SessionID;
                            break;
                        }
                    }
                }
            }
            finally
            {
                if (sessionInfos != IntPtr.Zero)
                    Wtsapi32.WTSFreeMemory(sessionInfos);
            }

            //If enumerating not working use the fall back method
            if (activeSessionId == Wtsapi32.InvalidSessionId)
            {
                activeSessionId = Kernel32.WTSGetActiveConsoleSessionId();

                if (activeSessionId == Wtsapi32.InvalidSessionId)
                    return null;
            }

            return GetPrimaryTokenFromSessionId(activeSessionId);
        }

        /// <summary>
        /// Gets the primary token from an first active user session with the given username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>A <see cref="SafeAccessTokenHandle"/> or null if no valid sessions was found.</returns>
        /// <exception cref="Win32Exception">
        /// </exception>
        public static SafeAccessTokenHandle GetPrimaryTokenByUsername(string username)
        {
            //!!! Parts are from Copyright (c) 2014 Justin Murray - MIT-License, thanks to him !!!
            // https://github.com/murrayju/CreateProcessAsUser, 18.08.2019

            uint activeSessionId = Wtsapi32.InvalidSessionId;
            IntPtr sessionInfos = IntPtr.Zero;
            var sessionCount = 0;

            try
            {
                //Get a handle to the user access token for the current active session.
                if (Wtsapi32.WTSEnumerateSessions(Wtsapi32.WtsCurrentServerHandle, 0, 1, ref sessionInfos,
                    ref sessionCount))
                {
                    int arrayElementSize = Marshal.SizeOf<Wtsapi32.WtsSessionInfo>();
                    IntPtr current = sessionInfos;

                    for (var i = 0; i < sessionCount; i++)
                    {
                        IntPtr usernamePtr = IntPtr.Zero;
                        IntPtr domainPtr = IntPtr.Zero;
                        try
                        {
                            var sessionInfo = Marshal.PtrToStructure<Wtsapi32.WtsSessionInfo>(current);
                            current += arrayElementSize;

                            //If the session is not active, go to the next
                            if (sessionInfo.State != Wtsapi32.WtsConnectstateClass.WTSActive)
                                continue;

                            //Get the username
                            if (!Wtsapi32.WTSQuerySessionInformation(Wtsapi32.WtsCurrentServerHandle, sessionInfo.SessionID, Wtsapi32.WtsInfoClass.WtsUserName, out usernamePtr, out uint bytesReturnedUsername))
                                throw new Win32Exception(Marshal.GetLastWin32Error());

                            //continue when no bytes returned
                            if (bytesReturnedUsername <= 0)
                                continue;

                            string sessionUsername = Marshal.PtrToStringUni(usernamePtr);

                            //Get the domain name
                            if (!Wtsapi32.WTSQuerySessionInformation(Wtsapi32.WtsCurrentServerHandle, sessionInfo.SessionID, Wtsapi32.WtsInfoClass.WtsDomainName, out domainPtr, out uint bytesReturnedDomainName))
                                throw new Win32Exception(Marshal.GetLastWin32Error());

                            //continue when no bytes returned
                            if (bytesReturnedDomainName <= 0)
                                continue;

                            string sessionDomainName = Marshal.PtrToStringUni(domainPtr);

                            //If the username is not the same, go to the next
                            string sessionFullUsername = sessionDomainName.ConvertNullTerminatedStringToString() + "\\" + sessionUsername.ConvertNullTerminatedStringToString();


                            //If the session is found break the for
                            if (!string.IsNullOrWhiteSpace(sessionFullUsername) && sessionFullUsername.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                activeSessionId = (uint)sessionInfo.SessionID;
                                break;
                            }
                        }
                        finally
                        {
                            if (usernamePtr != IntPtr.Zero)
                                Wtsapi32.WTSFreeMemory(usernamePtr);

                            if (domainPtr != IntPtr.Zero)
                                Wtsapi32.WTSFreeMemory(domainPtr);
                        }
                    }
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                if (sessionInfos != IntPtr.Zero)
                    Wtsapi32.WTSFreeMemory(sessionInfos);
            }

            //If enumerating not working throw an exception
            if (activeSessionId == Wtsapi32.InvalidSessionId)
                return null;

            return GetPrimaryTokenFromSessionId(activeSessionId);
        }

        /// <summary>
        /// Converts the user token to a primary token.
        /// </summary>
        /// <param name="userToken">The user token.</param>
        /// <returns>A primary token.</returns>
        /// <exception cref="Win32Exception"></exception>
        public static SafeAccessTokenHandle ConvertTokenToPrimaryToken(SafeAccessTokenHandle userToken)
        {
            if (!Advapi32.DuplicateTokenEx(userToken, 0, IntPtr.Zero, (int)Advapi32.SecurityImpersonationLevel.SecurityImpersonation, (int)Advapi32.TokenType.TokenPrimary, out SafeAccessTokenHandle primTokenWrapper))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return primTokenWrapper;
        }
    }
}
