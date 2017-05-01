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

namespace DaemonMasterService
{
    public partial class Service1 : ServiceBase
    {
        private const string regPath = @"SOFTWARE\DaemonMaster\Services\";
        private const int restartDelay = 2000;
        private const int waitToKillTime = 9500;

        private readonly string filePath = String.Empty;
        private readonly string parameter = String.Empty;
        private readonly string userName = String.Empty;
        private readonly string password = String.Empty;
        private readonly int maxRestarts = 0;
        private readonly bool noPause = true;
        private readonly bool noPowerEvents = true;


        private uint restarts = 0;

        Process process = null;



        public Service1(string serviceName)
        {
            InitializeComponent();

            try
            {
                //Open Regkey folder
                RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath + serviceName);

                //If the key doesn't exist
                if (key == null)
                    throw new Exception();

                filePath = (string)(key.GetValue("FilePath") ?? filePath);
                parameter = (string)(key.GetValue("Parameter") ?? parameter);
                userName = (string)(key.GetValue("UserName") ?? userName);
                password = (string)(key.GetValue("Password") ?? password);
                maxRestarts = (int)(key.GetValue("MaxRestarts") ?? maxRestarts);
                noPause = (bool)(key.GetValue("NoPause") ?? noPause);
                noPowerEvents = (bool)(key.GetValue("NoPowerEvents") ?? noPowerEvents);

                CanHandlePowerEvent = !noPowerEvents;
                CanPauseAndContinue = !noPause;
            }
            catch (Exception)
            {
                Stop();
            }
        }

        protected override void OnStart(string[] args)
        {
            StartProcess();
        }

        protected override void OnStop()
        {
            StopProcess();
        }

        protected override void OnPause()
        {
            PauseProcess();
        }

        protected override void OnContinue()
        {
            ResumeProcess();
        }


        private void StopProcess()
        {
            try
            {
                if (process != null)
                {
                    process.CloseMainWindow();
                    process.WaitForExit(waitToKillTime);
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
                if (filePath == String.Empty)
                    throw new Exception("Invalid filepath!");

                ProcessStartInfo startInfo = new ProcessStartInfo(filePath, parameter);

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
                if (maxRestarts == 0 || restarts < maxRestarts)
                {
                    Thread.Sleep(restartDelay);
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

        private int PauseProcess()
        {
            if (process == null)
                return 0;


            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, (uint)process.Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    bool value = KERNEL32.SuspendThread(processHandle);

                    if (value)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                finally
                {
                    KERNEL32.CloseHandle(processHandle);
                }
            }

            return -1;
        }

        private int ResumeProcess()
        {
            if (process == null)
                return 0;


            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, (uint)process.Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    bool value = KERNEL32.ResumeThread(processHandle);

                    if (value)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
                }
                finally
                {
                    KERNEL32.CloseHandle(processHandle);
                }
            }

            return -1;
        }
    }
}
