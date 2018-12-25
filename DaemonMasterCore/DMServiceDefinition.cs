/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceDefinition
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
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke.Advapi32;

namespace DaemonMasterCore
{
    public sealed class DmServiceDefinition : IWin32ServiceDefinition
    {
        /// <inheritdoc /> 
        public string ServiceName { get; set; }

        /// <inheritdoc /> 
        public string DisplayName { get; set; }

        /// <inheritdoc /> 
        public string BinaryPath { get; set; }

        /// <inheritdoc /> 
        public string Description { get; set; } = "";

        /// <inheritdoc /> 
        public Advapi32.ServiceStartType StartType { get; set; } = Advapi32.ServiceStartType.AutoStart;

        /// <inheritdoc /> 
        public bool DelayedStart { get; set; } = false;

        /// <inheritdoc /> 
        public string Arguments { get; set; } = "";

        /// <inheritdoc /> 
        public Advapi32.ErrorControl ErrorControl { get; set; } = Advapi32.ErrorControl.Normal;

        /// <inheritdoc />   
        public ServiceCredentials Credentials { get; set; } = ServiceCredentials.LocalSystem;

        /// <inheritdoc /> 
        public string[] DependOnService { get; set; } = Array.Empty<string>();

        /// <inheritdoc /> 
        public string[] DependOnGroup { get; set; } = Array.Empty<string>();

        /// <inheritdoc /> 
        public bool CanInteractWithDesktop { get; set; } = false;


        //----------------------------------------------------------------------------


        /// <summary>
        /// Get or set the maximum restart count of the process
        /// </summary>
        public int ProcessMaxRestarts { get; set; } = 3;

        /// <summary>
        /// Get or set the maxium time to wait for the shutdown of the process in ms
        /// </summary>
        public int ProcessTimoutTime { get; set; } = 9000;

        /// <summary>
        /// Get or set the delay time between a restart of the process in ms
        /// </summary>
        public int ProcessRestartDelay { get; set; } = 2000;

        /// <summary>
        /// Get or set the time after that the restart counter will be reset in minutes
        /// </summary>
        public int CounterResetTime { get; set; } = 1440; // default is 24h = 1440m

        //----------------------------------------------------------------------------

        /// <summary>
        /// Get or set the app type
        /// </summary>
        public bool IsConsoleApplication { get; set; } = false;

        /// <summary>
        /// Get or set the use of Ctrl-C (if its an console app)
        /// </summary>
        public bool UseCtrlC { get; set; } = false;

        //----------------------------------------------------------------------------
        //----------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="DmServiceDefinition"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service</param>
        public DmServiceDefinition(string serviceName)
        {
            ServiceName = serviceName;
        }
    }
}
