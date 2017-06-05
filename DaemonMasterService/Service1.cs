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
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace DaemonMasterService
{
    public partial class Service1 : ServiceBase
    {
        private Process process = null;
        private Daemon daemon = null;

        private uint restartCounter = 0;
        private Timer resetTimer = null;


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
            //Disable Process_Exited event
            process.EnableRaisingEvents = false;
            //Stop the process
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
            if (process == null || process.HasExited)
                return;

            //IntPtr handle = process.MainWindowHandle;

            //if (handle != IntPtr.Zero)
            //{
            //    USER32.PostMessage(handle, USER32._SYSCOMMAND, (IntPtr)USER32.wParam.SC_CLOSE, IntPtr.Zero);
            //}
            //else
            //{
            if (!process.CloseMainWindow())
            {
                //Schließt, wenn es eine Consolen Anwendung ist, das fenster mit einem Ctrl-C / Ctrl-Break Befehl
                if (daemon.ConsoleApplication)
                {
                    ProcessManagement.CloseConsoleApplication(daemon.UseCtrlC, (uint)process.Id);
                }
            }
            //}

            if (!process.WaitForExit(daemon.ProcessKillTime))
            {
                process.Kill();
            }

            process.Close();
            process.Dispose();
            process = null;
        }

        private void StartProcess()
        {
            try
            {
                if (daemon.FullPath == String.Empty)
                    throw new Exception("Invalid filepath!");

                ProcessStartInfo startInfo = new ProcessStartInfo(daemon.FullPath, daemon.Parameter);

                string extension = Path.GetExtension(daemon.FullPath);

                if (String.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.UseShellExecute = true;
                }
                else
                {
                    startInfo.UseShellExecute = false;
                }

                startInfo.ErrorDialog = false;

                process = new Process();
                process.StartInfo = startInfo;
                //Abboniert das Event
                process.Exited += Process_Exited;
                //Benötigt damit Exited funktioniert
                process.EnableRaisingEvents = true;
                process.Start();
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
                if (daemon.MaxRestarts == 0 || restartCounter < daemon.MaxRestarts)
                {

                    Timer restartDelayTimer = new Timer(o =>
                    {
                        process.Start();
                        restartCounter++;

                        if (resetTimer == null)
                        {
                            resetTimer = new Timer(ResetTimerCallback, null, daemon.CounterResetTime, Timeout.Infinite);
                        }
                        else
                        {
                            resetTimer.Change(daemon.CounterResetTime, Timeout.Infinite);
                        }

                        ((Timer)o).Dispose();

                    }, null, daemon.ProcessRestartDelay, Timeout.Infinite);
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

        private void ResetTimerCallback(object state)
        {
            restartCounter = 0;

            resetTimer.Dispose();
            resetTimer = null;
        }
    }
}
