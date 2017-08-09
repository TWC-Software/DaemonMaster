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
using System.ServiceProcess;
using DaemonMasterCore.Win32.PInvoke;
using NLog;

namespace DaemonMasterService
{
    public partial class Service : ServiceBase
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static string _serviceName = null;

        public Service(bool enablePause)
        {
            InitializeComponent();

            CanPauseAndContinue = enablePause;
            CanHandlePowerEvent = false;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            bool startInUserSession = false;

            //Read args
            switch (args[0])
            {
                case "-startInUserSession":
                    startInUserSession = true;
                    break;
            }

            //Get the name of the service
            _serviceName = DaemonMasterUtils.GetServiceName();

            //Change the filename of the log file to the service name
            LogManager.Configuration.Variables["logName"] = _serviceName;

            try
            {
                _logger.Info("Starting the process...");
                if (startInUserSession)
                {
                    Daemon daemon = RegistryManagement.LoadDaemonFromRegistry(_serviceName);
                    ProcessManagement.StartProcessAsUser(daemon.FullPath, daemon.Parameter, NativeMethods.PRIORITY_CLASS.NORMAL_PRIORITY_CLASS);
                }
                else
                {
                    switch (ProcessManagement.CreateNewProcess(_serviceName))
                    {
                        case ProcessManagement.DaemonProcessState.Successful:
                            _logger.Info("The start of the process was successful!");
                            break;

                        case ProcessManagement.DaemonProcessState.Unsuccessful:
                            _logger.Info("The start of the process was unsuccessful!");
                            break;

                        case ProcessManagement.DaemonProcessState.AlreadyStarted:
                            _logger.Info("The process is already started!");
                            break;
                    }
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
            switch (ProcessManagement.DeleteProcess(_serviceName))
            {
                case ProcessManagement.DaemonProcessState.Successful:
                    _logger.Info("The stop of the process was successful!");
                    break;

                case ProcessManagement.DaemonProcessState.Unsuccessful:
                    _logger.Warn("The stop of the process was unsuccessful! Killing the process...");
                    _logger.Info(ProcessManagement.KillAndDeleteProcess(_serviceName) ? "Successful!" : "Unsuccessful!");
                    break;

                case ProcessManagement.DaemonProcessState.AlreadyStopped:
                    _logger.Info("The process is already stopped!");
                    break;
            }

            base.OnStop();
        }

        protected override void OnPause()
        {
            _logger.Info("Suspending process thread...");
            _logger.Info(ProcessManagement.PauseProcess(_serviceName) ? "Successful!" : "Unsuccessful!");


            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            _logger.Info("Resuming suspend process thread...");
            _logger.Info(ProcessManagement.ResumeProcess(_serviceName) ? "Successful!" : "Unsuccessful!");
        }

        protected override void OnCustomCommand(int command)
        {
            if (command == (int)Commands.Kill)
            {
                ProcessManagement.KillAndDeleteProcess(_serviceName);
                Stop();
            }
        }

        public enum Commands : int
        {
            Kill = 128,
        }
    }
}
