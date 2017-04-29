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

                filePath = (string)key.GetValue("FilePath");
                parameter = (string)key.GetValue("Parameter");
                userName = (string)key.GetValue("UserName");
                password = (string)key.GetValue("Password");
                maxRestarts = (int)key.GetValue("MaxRestarts");
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
            try
            {
                if (process != null)
                {
                    process.CloseMainWindow();
                    process.WaitForExit(waitToKillTime);
                    process.Kill();

                    process.Dispose();
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
                    return;

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
    }
}
