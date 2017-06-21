/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SERVICE CODE 
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


using DaemonMasterCore;
using System;
using System.ServiceProcess;
using NLog;
using NLog.Targets;

namespace DaemonMasterService
{
    public partial class Service : ServiceBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static DaemonProcess _daemonProcess = null;

        public Service(bool enablePause)
        {
            InitializeComponent();

            CanPauseAndContinue = enablePause;
            CanHandlePowerEvent = false;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            //Get the name of the service
            string serviceName = DaemonMasterUtils.GetServiceName();

            //Change the filename of the log file to the service name
            LogManager.Configuration.Variables["logName"] = serviceName;

            try
            {
                //Load config from registry
                _daemonProcess = new DaemonProcess(serviceName);

                _logger.Info("Starting the process...");
                switch (_daemonProcess.StartProcess())
                {
                    case DaemonProcess.DaemonProcessState.Successful:
                        _logger.Info("The start of the process was successful!");
                        break;

                    case DaemonProcess.DaemonProcessState.Unsuccessful:
                        _logger.Info("The start of the process was unsuccessful!");
                        break;

                    case DaemonProcess.DaemonProcessState.AlreadyStarted:
                        _logger.Info("The process is already started!");
                        break;
                }
            }
            catch (Exception)
            {
                Stop();
            }
        }

        protected override void OnStop()
        {
            //Stop the process and give feedback in logs 
            switch (_daemonProcess.StopProcess())
            {
                case DaemonProcess.DaemonProcessState.Successful:
                    _logger.Info("The stop of the process was successful!");
                    break;

                case DaemonProcess.DaemonProcessState.Unsuccessful:
                    _logger.Warn("The stop of the process was unsuccessful! Killing the process...");
                    _daemonProcess.KillProcess();
                    break;

                case DaemonProcess.DaemonProcessState.AlreadyStopped:
                    _logger.Info("The process is already stopped!");
                    break;
            }

            _logger.Info("Dispose the process...");
            _daemonProcess.Dispose();

            base.OnStop();
        }

        protected override void OnPause()
        {
            _daemonProcess.PauseProcess();
            _logger.Info("Suspend process thread!");

            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            _daemonProcess.ResumeProcess();
            _logger.Info("Resume suspend process thread!");
        }
    }
}
