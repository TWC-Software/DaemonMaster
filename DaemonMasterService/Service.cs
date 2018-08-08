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
using System.ServiceProcess;
using DaemonMasterCore;
using Microsoft.Win32;
using NLog;

namespace DaemonMasterService
{
    public partial class Service : ServiceBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static string _serviceName;
        private bool _startInUserSession;
        private DaemonProcess _daemonProcess;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              CONST                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private const string RegPath = @"SYSTEM\CurrentControlSet\Services\";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             METHODS                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

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
                        _startInUserSession = true;
                        break;
                }
            }

            try
            {
                //Get the name of the service
                _serviceName = DaemonMasterUtils.GetServiceName();

                //Change the filename of the log file to the service name
                if (LogManager.Configuration != null)
                    LogManager.Configuration.Variables["logName"] = _serviceName;

                //create a new DaemonProcess object
                _daemonProcess = new DaemonProcess(_serviceName, _startInUserSession);


                Logger.Info("Starting the process...");
                switch (_daemonProcess.StartProcess())
                {
                    case DaemonProcessState.Successful:
                        UpdateInfosInRegistry(_serviceName, _daemonProcess.ProcessPID);
                        Logger.Info("The start of the process was successful!");
                        break;

                    case DaemonProcessState.Unsuccessful:
                        Logger.Info("The start of the process was unsuccessful!");
                        break;

                    case DaemonProcessState.AlreadyStarted:
                        Logger.Info("The process is already started!");
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString);
                Stop();
            }
        }

        protected override void OnStop()
        {
            //Stop the process and give feedback in logs 
            switch (_daemonProcess.StopProcess())
            {
                case DaemonProcessState.Successful:
                    Logger.Info("The stop of the process was successful!");
                    break;

                case DaemonProcessState.Unsuccessful:
                    Logger.Warn("The stop of the process was unsuccessful! Killing the process...");
                    Logger.Info(_daemonProcess.KillProcess() ? "Successful!" : "Unsuccessful!");
                    break;

                case DaemonProcessState.AlreadyStopped:
                    Logger.Info("The process is already stopped!");
                    break;
            }

            UpdateInfosInRegistry(_serviceName, _daemonProcess.ProcessPID);

            base.OnStop();
        }

        protected override void OnPause()
        {
            Logger.Info("Suspending process thread...");
            Logger.Info(_daemonProcess.PauseProcess() ? "Successful!" : "Unsuccessful!");

            UpdateInfosInRegistry(_serviceName, _daemonProcess.ProcessPID);

            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            UpdateInfosInRegistry(_serviceName, _daemonProcess.ProcessPID);

            Logger.Info("Resuming suspend process thread...");
            Logger.Info(_daemonProcess.ResumeProcess() ? "Successful!" : "Unsuccessful!");
        }

        protected override void OnCustomCommand(int command)
        {
            switch ((ServiceCommands)command)
            {
                case ServiceCommands.KillChildAndStop:
                    _daemonProcess.KillProcess();
                    Stop();
                    break;

                case ServiceCommands.UpdateInfos:
                    UpdateInfosInRegistry(_serviceName, _daemonProcess.ProcessPID);
                    break;
            }
        }

        private new void Stop()
        {
            _daemonProcess.Dispose();
            base.Stop();
        }

        private void UpdateInfosInRegistry(string serviceName, int processPid)
        {
            using (RegistryKey processKey = Registry.LocalMachine.CreateSubKey(RegPath + serviceName + @"\ProcessInfo"))
            {
                if (processKey == null)
                    return;

                processKey.SetValue("ProcessPID", processPid, RegistryValueKind.DWord);

                processKey.Close();
            }
        }
    }
}
