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
        public static bool IsProcessAlreadyThere(string serviceName)
        {
            return Processes.ContainsKey(serviceName);
        }

        /// <summary>
        /// Create a new process with the service name (return the process object), if a process exists with the same service name, the function return null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static DaemonProcess CreateNewProcess(string serviceName)
        {
            if (IsProcessAlreadyThere(serviceName))
                return null;

            DaemonProcess process = new DaemonProcess(serviceName);
            Processes.Add(serviceName, process);
            return Processes[serviceName];
        }

        /// <summary>
        /// Dispose the process with the given service name
        /// </summary>
        /// <param name="serviceName"></param>
        public static int DeleteProcess(string serviceName)
        {
            if (!IsProcessAlreadyThere(serviceName))
                return -1;

            if (Processes[serviceName].StopProcess() != 1)
            {
                Processes[serviceName].Dispose();
                Processes.Remove(serviceName);
                return 1;
            }
            return 0;
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
    }
}
