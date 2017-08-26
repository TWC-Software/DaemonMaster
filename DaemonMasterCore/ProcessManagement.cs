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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
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

namespace DaemonMasterCore
{
    public static class ProcessManagement
    {
        private static readonly Dictionary<string, DaemonProcess> Processes = new Dictionary<string, DaemonProcess>();

        [Description("Only for services that run under the LocalSystem")]
        public static Process StartProcessAsUser(string filePath, string arguments)
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
            NativeMethods.SECURITY_ATTRIBUTES securityAttributes = new NativeMethods.SECURITY_ATTRIBUTES();
            securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

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
                        ref securityAttributes,
                        ref securityAttributes,
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



        /// <summary>
        /// Get the Process object of the given service name, if no process exists to the given service name the function return null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        //public static DaemonProcess GetProcessByName(string serviceName)
        //{
        //    if (IsProcessAlreadyThere(serviceName))
        //    {
        //        return Processes[serviceName];
        //    }
        //    return null;
        //}

        /// <summary>
        /// Check if the Process with the given service name already exists
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        private static bool IsProcessAlreadyThere(string serviceName)
        {
            return Processes.ContainsKey(serviceName);
        }

        /// <summary>
        /// Create a new process with the service name (return the process object)
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static DaemonProcessState CreateNewProcess(string serviceName, bool startProcessAsUser = false)
        {
            if (IsProcessAlreadyThere(serviceName))
                return DaemonProcessState.AlreadyStarted;

            DaemonProcess process = new DaemonProcess(serviceName, startProcessAsUser);
            DaemonProcessState result = process.StartProcess();

            switch (result)
            {
                case DaemonProcessState.AlreadyStarted:
                    break;

                case DaemonProcessState.Successful:
                    Processes.Add(serviceName, process);
                    break;

                case DaemonProcessState.Unsuccessful:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Dispose the process with the given service name
        /// </summary>
        /// <param name="serviceName"></param>
        public static DaemonProcessState DeleteProcess(string serviceName)
        {
            if (!IsProcessAlreadyThere(serviceName))
                return DaemonProcessState.AlreadyStopped;

            DaemonProcess process = Processes[serviceName];
            DaemonProcessState result = process.StopProcess();

            switch (result)
            {
                case DaemonProcessState.AlreadyStopped:
                    break;

                case DaemonProcessState.Successful:
                    process.Dispose();
                    Processes.Remove(serviceName);
                    break;

                case DaemonProcessState.Unsuccessful:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Kill and Dispose the process with the given service name
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static bool KillAndDeleteProcess(string serviceName)
        {
            if (!IsProcessAlreadyThere(serviceName))
                return false;

            Processes[serviceName].KillProcess();
            Processes[serviceName].Dispose();
            Processes.Remove(serviceName);
            return true;
        }

        /// <summary>
        /// Pause the process
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static bool PauseProcess(string serviceName)
        {
            if (!IsProcessAlreadyThere(serviceName))
                return false;

            return Processes[serviceName].PauseProcess();
        }

        /// <summary>
        /// Resume the process
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static bool ResumeProcess(string serviceName)
        {
            if (!IsProcessAlreadyThere(serviceName))
                return false;

            return Processes[serviceName].ResumeProcess();
        }

        /// <summary>
        /// Kill all processes in the list
        /// </summary>
        public static void KillAndDeleteAllProcesses()
        {
            foreach (var process in Processes)
            {
                try
                {
                    DaemonProcess daemonProcess = process.Value;

                    daemonProcess.KillProcess();
                    daemonProcess.Dispose();
                }
                catch (Exception)
                {
                    continue;
                }
            }

            //Clear process list
            Processes.Clear();
        }

        /// <summary>
        /// If dictionary/list empty
        /// </summary>
        /// <returns></returns>
        public static bool IsDictionaryEmpty()
        {
            return Processes.Count > 1;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              Other                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public enum DaemonProcessState
        {
            AlreadyStopped,
            AlreadyStarted,
            Successful,
            Unsuccessful,
        }
    }
}
