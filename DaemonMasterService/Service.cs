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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////


using DaemonMasterCore;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using DaemonMasterCore.Win32.PInvoke;
using NLog;

namespace DaemonMasterService
{
    public partial class Service : ServiceBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static string _serviceName = null;
        private bool startInUserSession = false;
        private DaemonProcess _daemonProcess = null;

        public Service(bool enablePause)
        {
            InitializeComponent();

            CanPauseAndContinue = enablePause;
            CanHandlePowerEvent = false;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            //Read args
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-startInUserSession":
                        startInUserSession = true;
                        break;
                }
            }

            try
            {
                //Get the name of the service
                _serviceName = DaemonMasterUtils.GetServiceName();

                //Change the filename of the log file to the service name
                LogManager.Configuration.Variables["logName"] = _serviceName;

                //create a new DaemonProcess object
                _daemonProcess = new DaemonProcess(_serviceName, startInUserSession);


                _logger.Info("Starting the process...");
                switch (_daemonProcess.StartProcess())
                {
                    case DaemonProcessState.Successful:
                        _logger.Info("The start of the process was successful!");
                        break;

                    case DaemonProcessState.Unsuccessful:
                        _logger.Info("The start of the process was unsuccessful!");
                        break;

                    case DaemonProcessState.AlreadyStarted:
                        _logger.Info("The process is already started!");
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString);
                Stop();
            }
        }

        protected override void OnStop()
        {
            //Stop the process and give feedback in logs 
            switch (_daemonProcess.StopProcess())
            {
                case DaemonProcessState.Successful:
                    _logger.Info("The stop of the process was successful!");
                    break;

                case DaemonProcessState.Unsuccessful:
                    _logger.Warn("The stop of the process was unsuccessful! Killing the process...");
                    _logger.Info(_daemonProcess.KillProcess() ? "Successful!" : "Unsuccessful!");
                    break;

                case DaemonProcessState.AlreadyStopped:
                    _logger.Info("The process is already stopped!");
                    break;
            }

            base.OnStop();
        }

        protected override void OnPause()
        {
            _logger.Info("Suspending process thread...");
            _logger.Info(_daemonProcess.PauseProcess() ? "Successful!" : "Unsuccessful!");


            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            _logger.Info("Resuming suspend process thread...");
            _logger.Info(_daemonProcess.ResumeProcess() ? "Successful!" : "Unsuccessful!");
        }

        protected override void OnCustomCommand(int command)
        {
            switch ((ServiceCommands)command)
            {
                case ServiceCommands.KillChildAndStop:
                    _daemonProcess.KillProcess();
                    Stop();
                    break;
            }
        }

        private new void Stop()
        {
            _daemonProcess.Dispose();
            base.Stop();
        }
    }
}
