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
using System.Timers;
using DaemonMaster.Core;
using DaemonMaster.Core.Config;
using Microsoft.Win32;
using NLog;

namespace DaemonMasterService
{
    public partial class Service : ServiceBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string _serviceName;
        private bool _startInUserSession;
        private DmProcess _dmProcess;
        private Timer _updateTimer;
        private Config _config;

        private uint _oldProcessPid;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              CONST                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private const string RegPath = @"SYSTEM\CurrentControlSet\Services\";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             METHODS                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            //Get the config
            _config = ConfigManagement.GetConfig;

            //Read args
            foreach (string arg in args)
            {
                if (arg == "-startInUserSession") _startInUserSession = true;
            }

            //Get the service name
            _serviceName = GetServiceName();

            try
            {
                //Create a Timer to update the actual status of the service
                _updateTimer = new Timer(_config.UpdateInterval);
                _updateTimer.Elapsed += UpdateTimerOnElapsed;
                _updateTimer.Enabled = true;


                //Change the filename of the log file to the service name
                if (LogManager.Configuration != null)
                    LogManager.Configuration.Variables["logName"] = _serviceName;

                //Create a new DmProcess instance with reg data
                _dmProcess = new DmProcess(RegistryManagement.LoadServiceStartInfosFromRegistry(_serviceName));
                _dmProcess.MaxRestartsReached += DmProcessOnMaxRestartsReached;
                _dmProcess.UpdateProcessPid += DmProcessOnUpdateProcessPid;

                Logger.Info("Starting the process...");
                if (_startInUserSession)
                {
                    _dmProcess.StartInUserSession();
                }
                else
                {
                    _dmProcess.StartInServiceSession();
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
                _dmProcess.StopProcess();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                Stop();
            }

            base.OnStop();
        }


        private void DmProcessOnMaxRestartsReached(object sender, EventArgs e)
        {
            Stop();
        }

        private void DmProcessOnUpdateProcessPid(object sender, EventArgs e)
        {
            UpdateProcessPid();
        }

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateProcessPid();
        }

        /// <summary>
        /// Updates the process pid.
        /// </summary>
        private void UpdateProcessPid()
        {
            try
            {
                if (_oldProcessPid == _dmProcess.ProcessPid)
                    return;

                using (RegistryKey processKey = Registry.LocalMachine.OpenSubKey(RegPath + _serviceName + @"\ProcessInfo", true))
                {
                    if (processKey == null)
                        return;

                    processKey.SetValue("ProcessPid", _dmProcess.ProcessPid, RegistryValueKind.DWord);

                    processKey.Close();
                }

                _oldProcessPid = _dmProcess.ProcessPid;
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
    }
}
