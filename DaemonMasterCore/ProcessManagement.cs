/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: PROCESS MANAGEMENT FILE
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

namespace DaemonMasterCore
{
    public static class ProcessManagement
    {
        private static readonly Dictionary<string, DaemonProcess> Processes = new Dictionary<string, DaemonProcess>();

        /// <summary>
        /// Get the Process object of the given service name, if no process exists to the given service name the function return null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static DaemonProcess GetProcessByName(string serviceName)
        {
            if (IsProcessAlreadyThere(serviceName))
            {
                return Processes[serviceName];
            }

            return null;
        }

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
        public static DaemonProcess.DaemonProcessState CreateNewProcess(string serviceName)
        {
            if (IsProcessAlreadyThere(serviceName))
                return DaemonProcess.DaemonProcessState.AlreadyStarted;

            DaemonProcess process = new DaemonProcess(serviceName);
            DaemonProcess.DaemonProcessState result = process.StartProcess();

            switch (result)
            {
                case DaemonProcess.DaemonProcessState.AlreadyStarted:
                    break;

                case DaemonProcess.DaemonProcessState.Successful:
                    Processes.Add(serviceName, process);
                    break;

                case DaemonProcess.DaemonProcessState.Unsuccessful:
                    break;
            }

            return result;
        }

        /// <summary>
        /// Dispose the process with the given service name
        /// </summary>
        /// <param name="serviceName"></param>
        public static DaemonProcess.DaemonProcessState DeleteProcess(string serviceName)
        {
            if (!IsProcessAlreadyThere(serviceName))
                return DaemonProcess.DaemonProcessState.AlreadyStopped;

            DaemonProcess process = new DaemonProcess(serviceName);
            DaemonProcess.DaemonProcessState result = process.StopProcess();

            switch (result)
            {
                case DaemonProcess.DaemonProcessState.AlreadyStopped:
                    break;

                case DaemonProcess.DaemonProcessState.Successful:
                    process.Dispose();
                    Processes.Remove(serviceName);
                    break;

                case DaemonProcess.DaemonProcessState.Unsuccessful:
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
            return Processes.Count > 0;
        }
    }
}
