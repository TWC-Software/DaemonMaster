/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: PROCESS FILE
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaemonMasterCore.Win32;
using System.Threading;

namespace DaemonMasterCore
{
    public class Process
    {
        private readonly Daemon _daemon = null;

        private System.Diagnostics.Process _process = null;
        private Timer _resetTimer = null;
        private uint _restartCounter = 0;

        public Process(Daemon daemon)
        {
            _daemon = daemon;
        }

        public Process(string serviceName)
        {
            _daemon = RegistryManagement.LoadDaemonFromRegistry(serviceName);
        }


        public Daemon GetDaemon => _daemon;

        public bool CloseConsoleApplication(bool useCtrlC)
        {
            if (_process == null)
                throw new NullReferenceException();


            if (useCtrlC)
            {
                return NativeMethods.GenerateConsoleCtrlEvent((uint)NativeMethods.CtrlEvent.CTRL_C_EVENT, (uint)_process.Id);
            }
            else
            {
                return NativeMethods.GenerateConsoleCtrlEvent((uint)NativeMethods.CtrlEvent.CTRL_BREAK_EVENT, (uint)_process.Id);
            }
        }

        public bool PauseProcess()
        {
            if (_process == null)
                throw new NullReferenceException();

            IntPtr processHandle = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, true, (uint)_process.Id);

            if (processHandle == IntPtr.Zero)
                return false;

            try
            {
                return NativeMethods.SuspendThread(processHandle);
            }
            finally
            {
                NativeMethods.CloseHandle(processHandle);
            }
        }

        public bool ResumeProcess()
        {
            if (_process == null)
                throw new NullReferenceException();

            IntPtr processHandle = NativeMethods.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, true, (uint)_process.Id);

            if (processHandle == IntPtr.Zero)
                return false;

            try
            {
                return NativeMethods.ResumeThread(processHandle);
            }
            finally
            {
                NativeMethods.CloseHandle(processHandle);
            }
        }

        public bool StartProcess()
        {
            if (_daemon.FullPath == String.Empty)
                throw new Exception("Invalid filepath!");

            ProcessStartInfo startInfo = new ProcessStartInfo(_daemon.FullPath, _daemon.Parameter);
            startInfo.ErrorDialog = false;

            //UseShellExecute if the application is a shortcut
            string extension = Path.GetExtension(_daemon.FullPath);

            if (String.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.UseShellExecute = true;
            }
            else
            {
                startInfo.UseShellExecute = false;
            }

            //Create an _process object
            _process = new System.Diagnostics.Process();
            _process.StartInfo = startInfo;
            //Subscribe to the event
            _process.Exited += Process_Exited;
            //Required for that Exited event work
            _process.EnableRaisingEvents = true;
            _process.Start();
            return true;
        }

        public bool StopProcess()
        {
            if (_process == null || _process.HasExited)
                throw new NullReferenceException();

            //Disable Process_Exited event
            _process.EnableRaisingEvents = false;

            //IntPtr handle = _process.MainWindowHandle;
            //NativeMethods.PostMessage(handle, NativeMethods._SYSCOMMAND, (IntPtr)NativeMethods.wParam.SC_CLOSE, IntPtr.Zero);

            //Send Ctrl-C / Ctrl-Break command if the application is a console
            if (_daemon.ConsoleApplication)
            {
                CloseConsoleApplication(_daemon.UseCtrlC);
            }

            //Close the MainWindow of the application 
            _process.CloseMainWindow();

            //Waiting for the _process to close, after a ProcessKillTime the _process will be killed
            if (!_process.WaitForExit(_daemon.ProcessKillTime))
            {
                _process.Kill();
            }

            _process.Close();
            _process.Dispose();
            _process = null;
            return true;
        }




        private void Process_Exited(object sender, EventArgs args)
        {
            try
            {
                if (_daemon.MaxRestarts == 0 || _restartCounter < _daemon.MaxRestarts)
                {

                    Timer restartDelayTimer = new Timer(o =>
                    {
                        _process.Start();
                        _restartCounter++;

                        if (_resetTimer == null)
                        {
                            _resetTimer = new Timer(ResetTimerCallback, null, _daemon.CounterResetTime, Timeout.Infinite);
                        }
                        else
                        {
                            _resetTimer.Change(_daemon.CounterResetTime, Timeout.Infinite);
                        }

                        ((Timer)o).Dispose();

                    }, null, _daemon.ProcessRestartDelay, Timeout.Infinite);
                }
                else
                {
                    //Stop();
                }
            }
            catch (Exception)
            {
                //Stop();
            }
        }

        private void ResetTimerCallback(object state)
        {
            _restartCounter = 0;

            _resetTimer.Dispose();
            _resetTimer = null;
        }
    }
}
