/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ProcessManagement
//  
//  This file is part of DeamonMaster.
// 
//  DeamonMaster is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//   DeamonMaster is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
using NativeMethods = DaemonMasterCore.Win32.PInvoke.NativeMethods;

namespace DaemonMasterCore
{
    public static class ProcessManagement
    {
        [Description("Only for services that run under the LocalSystem")]
        internal static Process StartProcessAsUser(string filePath, string arguments)
        {
            string fileDir = Path.GetDirectoryName(filePath);

            //Set the startinfos (like desktop)
            NativeMethods.STARTUPINFO startupInfo = new NativeMethods.STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = @"winsta0\default";

            NativeMethods.PROCESS_INFORMATION processInformation = new NativeMethods.PROCESS_INFORMATION();

            //Flags that specify the priority and creation method of the process
            int creationFlags = (int)NativeMethods.PRIORITY_CLASS.NORMAL_PRIORITY_CLASS | NativeMethods.CREATE_NEW_CONSOLE;

            //Set only the length to inherit the security attributes of the existing token
            //NativeMethods.SECURITY_ATTRIBUTES securityAttributes = new NativeMethods.SECURITY_ATTRIBUTES();
            //securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

            //Get user session ID
            uint currentUserSessionId = NativeMethods.WTSGetActiveConsoleSessionId();

            //Get user token
            using (TokenHandle currentUserToken = TokenHandle.GetTokenFromSessionID(currentUserSessionId))
            {
                try
                {
                    if (!NativeMethods.CreateProcessAsUser(
                        currentUserToken,
                        null,
                        BuildCommandLineString(filePath, arguments),
                        null,
                        null,
                        false,
                        creationFlags,
                        IntPtr.Zero,
                        fileDir,
                        ref startupInfo,
                        out processInformation))
                    {
                        throw new Win32Exception("CreateProcessAsUser:" + Marshal.GetLastWin32Error());
                    }

                    return Process.GetProcessById((int)processInformation.dwProcessId);
                }
                finally
                {
                    if (processInformation.hProcess != IntPtr.Zero)
                        NativeMethods.CloseHandle(processInformation.hProcess);

                    if (processInformation.hThread != IntPtr.Zero)
                        NativeMethods.CloseHandle(processInformation.hThread);
                }
            }
        }

        [Description("Check if the string is quoted, if not it do it here")]
        private static StringBuilder BuildCommandLineString(string filePath, string arguments)
        {
            StringBuilder stringBuilder = new StringBuilder();
            filePath = filePath.Trim();

            bool filePathIsQuoted = filePath.StartsWith("\"", StringComparison.Ordinal) && filePath.EndsWith("\"", StringComparison.Ordinal);
            if (!filePathIsQuoted)
                stringBuilder.Append("\"");

            stringBuilder.Append(filePath);

            if (!filePathIsQuoted)
                stringBuilder.Append("\"");

            //Adds arguments to the StringBuilder
            if (!String.IsNullOrEmpty(arguments))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(arguments);
            }

            return stringBuilder;
        }


    }
}
