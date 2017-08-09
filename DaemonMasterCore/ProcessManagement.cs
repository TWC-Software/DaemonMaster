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
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;

namespace DaemonMasterCore
{
    public static class ProcessManagement
    {
        private static readonly Dictionary<string, DaemonProcess> Processes = new Dictionary<string, DaemonProcess>();

        // TODO Make the function complete
        public static void StartProcessAsUser(string filePath, string args, NativeMethods.PRIORITY_CLASS priority)
        {

            string fileDir = Path.GetDirectoryName(filePath);

            //Set only the length to inherit the security attributes of the existing token
            NativeMethods.SECURITY_ATTRIBUTES securityAttributes = new NativeMethods.SECURITY_ATTRIBUTES();
            securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

            // flags that specify the priority and creation method of the process
            uint creationFlags = (uint)priority | NativeMethods.CREATE_NEW_CONSOLE;

            NativeMethods.STARTUPINFO startupinfo = new NativeMethods.STARTUPINFO();
            startupinfo.cb = Marshal.SizeOf(startupinfo);
            startupinfo.dwFlags = (uint)NativeMethods.STARTINFO_FLAGS.STARTF_USESHOWWINDOW;
            startupinfo.wShowWindow = (short)NativeMethods.WINDOW_SHOW_STYLE.SW_SHOW;
            startupinfo.lpTitle = null;


            //Get user session ID
            uint currentUserSessionId = NativeMethods.WTSGetActiveConsoleSessionId();

            //Get user token
            using (TokenHandle currentUserToken = TokenHandle.GetTokenFromSessionID(currentUserSessionId))
            {
                NativeMethods.PROCESS_INFORMATION processInformation = new NativeMethods.PROCESS_INFORMATION();

                try
                {
                    if (!NativeMethods.CreateProcessAsUser(
                        currentUserToken,
                        null,
                        String.Format("\"{0}\" {1}", filePath.Replace(@"\", @"\\"), args),
                    ref securityAttributes,
                        ref securityAttributes,
                        false,
                        creationFlags,
                        IntPtr.Zero,
                        fileDir,
                        ref startupinfo,
                        out processInformation))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
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
        public static DaemonProcessState CreateNewProcess(string serviceName)
        {
            if (IsProcessAlreadyThere(serviceName))
                return DaemonProcessState.AlreadyStarted;

            DaemonProcess process = new DaemonProcess(serviceName);
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
