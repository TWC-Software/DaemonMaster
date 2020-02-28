/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: Service
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
using System.Management;
using System.ServiceProcess;
using DaemonMaster.Core;
using Microsoft.Win32;
using NLog;
using NLog.Targets;

namespace DaemonMasterService
{
    public partial class Service : ServiceBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string _serviceName;
        private DmProcess _dmProcess;
        private uint _oldProcessPid;
        private DmServiceDefinition _serviceDefinition;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              CONST                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private const string RegPath = @"SYSTEM\CurrentControlSet\Services\";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        SERVICE METHODS                                               //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            try
            {
                //Get the service name
                _serviceName = GetServiceName();

                //Get data from registry 
                _serviceDefinition = RegistryManagement.LoadServiceStartInfosFromRegistry(_serviceName);

                //--------------------------------------------------------------------

                //Setup NLOG for the service
                if (_serviceDefinition.UseEventLog)
                    SetupEventLogService();

                //Request additional time
                RequestAdditionalTime(_serviceDefinition.ProcessTimeoutTime + 1000);

                //Create a new DmProcess instance with reg data
                _dmProcess = new DmProcess(_serviceDefinition);
                _dmProcess.MaxRestartsReached += DmProcessOnMaxRestartsReached;
                _dmProcess.UpdateProcessPid += DmProcessOnUpdateProcessPid;

                //Check if the service should start in a user session or in the service session
                string sessionUsername = RegistryManagement.ReadAndClearSessionUsername(_serviceName);
                if (string.IsNullOrWhiteSpace(sessionUsername))
                {
                    Logger.Info("Starting the process in service session...");
                    _dmProcess.StartInServiceSession();
                }
                else
                {
                    Logger.Info("Starting the process in user session...");
                    _dmProcess.StartInUserSession(sessionUsername);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                Stop();
            }
        }

        protected override void OnStop()
        {
            try
            {
                //Request additional time
                if (_serviceDefinition != null)
                    RequestAdditionalTime(_serviceDefinition.ProcessTimeoutTime + 1000);

                _dmProcess.StopProcess();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                Stop();
            }

            base.OnStop();
        }

        protected override void OnCustomCommand(int command)
        {
            switch (command)
            {
                case (int)ServiceCommands.ServiceKillProcessAndStop:
                    if (_dmProcess != null && _dmProcess.KillProcess())
                    {
                        Stop();
                    }
                    else
                    {
                        Logger.Error("OnCustomCommand: Failed to kill process.");
                    }
                    break;

                case (int)ServiceCommands.ServiceKillProcess:
                    if (_dmProcess != null && !_dmProcess.KillProcess())
                    {
                        Logger.Error("OnCustomCommand: Failed to kill process.");
                    }
                    break;

                default:
                    Logger.Error("OnCustomCommand: command not found!");
                    break;
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             OTHER                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void DmProcessOnMaxRestartsReached(object sender, EventArgs e)
        {
            Stop();
        }

        private void DmProcessOnUpdateProcessPid(object sender, uint pid)
        {
            try
            {
                if (_oldProcessPid == pid)
                    return;

                using (RegistryKey processKey = Registry.LocalMachine.OpenSubKey(RegPath + _serviceName + @"\ProcessInfo", true))
                {
                    if (processKey == null)
                        return;

                    processKey.SetValue("ProcessPid", pid, RegistryValueKind.DWord);

                    processKey.Close();
                }

                _oldProcessPid = pid;
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Error(ex, ex.Message);

                //do nothing other, so that the service can run even when this key is not set (not necessary key)
            }
        }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Can't get the service name.</exception>
        private static string GetServiceName()
        {
            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            var searcher = new ManagementObjectSearcher(query);

            foreach (ManagementBaseObject o in searcher.Get())
            {
                var queryObj = (ManagementObject)o;
                return queryObj["Name"].ToString();
            }

            throw new Exception("Can't get the service name.");
        }

        private static void SetupEventLogService()
        {
            if (LogManager.Configuration == null)
                return;

            //Create targets and adding rules
            var serviceEventLogTarget = new EventLogTarget("eventLogTarget")
            {
                Layout = "Service: " + _serviceName + "\n" + @"${date:format=HH\:mm\:ss} ${level:uppercase=true} ${message} ${exception}",
                OptimizeBufferReuse = true,
                Source = EventLogManager.EventSource,
                Name = EventLogManager.EventLogName
            };

            LogManager.Configuration.AddTarget(serviceEventLogTarget);

#if DEBUG
            LogManager.Configuration.AddRule(LogLevel.Info, LogLevel.Fatal, serviceEventLogTarget);// only infos and higher
#else
            LogManager.Configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, serviceEventLogTarget);// only infos and higher
#endif

            LogManager.ReconfigExistingLoggers();
        }
    }
}
