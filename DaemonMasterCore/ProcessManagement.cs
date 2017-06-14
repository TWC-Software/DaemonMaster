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
using System.Linq;

namespace DaemonMasterCore
{
    public static class ProcessManagement
    {
        private static List<KeyValuePair<string, Process>> processes = new List<KeyValuePair<string, Process>>();

        /// <summary>
        /// Get the Process object of the given service name, if no process exists to the given service name the function return null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static Process GetProcessByName(string serviceName)
        {
            if (IsProcessAlreadyThere(serviceName))
            {
                return processes.FirstOrDefault(x => x.Key == serviceName).Value;
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
            if (processes.FirstOrDefault(x => x.Key == serviceName).Equals(default(KeyValuePair<string, Process>)))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Create a new process with the service name (return the process object), if a process exists with the same service name, the function return null
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static Process CreateNewProcces(string serviceName)
        {
            if (IsProcessAlreadyThere(serviceName))
                return null;

            Process process = new Process(serviceName);
            processes.Add(new KeyValuePair<string, Process>(serviceName, process));
            return processes.FirstOrDefault(x => x.Key == serviceName).Value;
        }
    }
}
