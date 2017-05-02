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


using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;
using DaemonMasterService.Win32;
using DaemonMasterService.Core;

namespace DaemonMasterService
{
    public partial class Service1 : ServiceBase
    {
        Process process = null;
        Config config = null;

        private uint restarts = 0;


        public Service1(bool enablePause)
        {
            InitializeComponent();

            CanPauseAndContinue = enablePause;
            CanHandlePowerEvent = false;
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            //Load config from registry
            config = DaemonMasterServiceCore.GetConfigFromRegistry(DaemonMasterServiceCore.GetServiceName());

            //Stop when config is null
            if (config == null)
                Stop();

            StartProcess();
        }

        protected override void OnStop()
        {
            StopProcess();

            base.OnStop();
        }

        protected override void OnPause()
        {

            DaemonMasterServiceCore.PauseProcess((uint)process.Id);

            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            DaemonMasterServiceCore.ResumeProcess((uint)process.Id);
        }


        private void StopProcess()
        {
            try
            {
                if (process != null)
                {
                    process.CloseMainWindow();

                    if (config.ConsoleApplication)
                    {
                        DaemonMasterServiceCore.CloseConsoleApplication(config.UseCtrlC, (uint)process.Id);
                    }

                    process.WaitForExit(config.ProcessKillTime);
                    process.Kill();

                    process.Dispose();
                    process = null;
                }
            }
            catch (Exception)
            {
            }
        }

        private void StartProcess()
        {
            try
            {
                if (config.FullPath == String.Empty)
                    throw new Exception("Invalid filepath!");

                ProcessStartInfo startInfo = new ProcessStartInfo(config.FullPath, config.Parameter);

                //if (userName != String.Empty && password != String.Empty)
                //{
                //    startInfo.UserName = userName;
                //    startInfo.Password = password;
                //}

                process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                //Abboniert das Event
                process.Exited += Process_Exited;
                //Benötigt damit Exited funktioniert
                process.EnableRaisingEvents = true;


            }
            catch (Exception)
            {
                Stop();
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            try
            {
                if (config.MaxRestarts == 0 || restarts < config.MaxRestarts)
                {
                    Thread.Sleep(config.ProcessRestartDelay);
                    process.Start();
                    restarts++;
                }
                else
                {
                    Stop();
                }
            }
            catch (Exception)
            {
                Stop();
            }
        }
    }
}
