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
using DaemonMasterCore;
using DaemonMasterCore.Win32;
using System.Runtime.InteropServices;

namespace DaemonMasterService
{
    public partial class Service1 : ServiceBase
    {
        Process process = null;
        Daemon daemon = null;

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

            try
            {
                //Load config from registry
                daemon = RegistryManagement.LoadDaemonFromRegistry(DaemonMasterUtils.GetServiceName());
                StartProcess();
            }
            catch (Exception)
            {
                Stop();
            }
        }

        protected override void OnStop()
        {
            StopProcess();

            base.OnStop();
        }

        protected override void OnPause()
        {
            ProcessManagement.PauseProcess((uint)process.Id);

            base.OnPause();
        }

        protected override void OnContinue()
        {
            base.OnContinue();

            ProcessManagement.ResumeProcess((uint)process.Id);
        }


        private void StopProcess()
        {
            try
            {
                if (process != null)
                {
                    IntPtr handle = process.MainWindowHandle;

                    if (handle != IntPtr.Zero)
                    {
                        USER32.PostMessage(handle, USER32._SYSCOMMAND, (IntPtr)USER32.wParam.SC_CLOSE, IntPtr.Zero);
                    }
                    else
                    {
                        if (!process.CloseMainWindow())
                        {
                            //Schließt, wenn es eine Consolen Anwendung ist, das fenster mit einem Ctrl-C / Ctrl-Break Befehl
                            if (daemon.ConsoleApplication)
                            {
                                ProcessManagement.CloseConsoleApplication(daemon.UseCtrlC, (uint)process.Id);
                            }
                        }
                    }

                    process.WaitForExit(daemon.ProcessKillTime);
                    process.Kill();

                    process.Close();
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
                if (daemon.FullPath == String.Empty)
                    throw new Exception("Invalid filepath!");

                ProcessStartInfo startInfo = new ProcessStartInfo(daemon.FullPath, daemon.Parameter);


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
                if (daemon.MaxRestarts == 0 || restarts < daemon.MaxRestarts)
                {
                    Thread.Sleep(daemon.ProcessRestartDelay);
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
