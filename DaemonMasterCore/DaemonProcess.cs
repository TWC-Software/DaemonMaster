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

using DaemonMasterCore.Win32;
using System;
using System.Diagnostics;
using System.Threading;

namespace DaemonMasterCore
{
    public class DaemonProcess : IDisposable
    {
        private readonly Daemon _daemon = null;
        private readonly Process _process = new Process();

        //Don't change!!
        private int _restarts = 0;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Constructor + Init                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor/Init

        public DaemonProcess(string serviceName)
        {
            _daemon = RegistryManagement.LoadDaemonFromRegistry(serviceName);
            Init();
        }

        public void Init()
        {
            //Create the start info for the process
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = _daemon.FullPath,
                Arguments = _daemon.Parameter,
                UseShellExecute = true //For .ink
            };

            _process.StartInfo = startInfo;
            //Enable raising events for auto restart
            _process.EnableRaisingEvents = true;
            _process.Exited += ProcessOnExited;
        }
        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                  Start, Stop, Pause, Resume, etc                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Start/Stop/Pause/Resume/etc

        public DaemonProcessState StartProcess()
        {
            if (_process.Start())
            {
                return DaemonProcessState.Successful;
            }

            return DaemonProcessState.Unsuccessful;
        }

        public DaemonProcessState StopProcess()
        {
            //If process already stoppend return
            if (!IsRunning())
                return DaemonProcessState.AlreadyStopped;

            //Disable raising events (disable auto restart)
            _process.EnableRaisingEvents = false;

            try
            {
                //If console app then send Ctrl-C or Ctrl-Break command
                if (_daemon.ConsoleApplication)
                {
                    CloseConsoleApplication(_daemon.UseCtrlC);
                }

                //Send close main window command
                _process.CloseMainWindow();

                //Wait for a defined time
                if (_process.WaitForExit(_daemon.ProcessKillTime))
                {
                    _process.Close();
                    return DaemonProcessState.Successful;
                }

                return DaemonProcessState.Unsuccessful;
            }
            catch (Exception)
            {
                return DaemonProcessState.Unsuccessful;
            }
        }

        public DaemonProcessState KillProcess()
        {
            //If process already stoppend return
            if (!IsRunning())
                return DaemonProcessState.AlreadyStopped;

            //Disable raising events (disable auto restart)
            _process.EnableRaisingEvents = false;

            //Kill the process
            try
            {
                _process.Kill();
                _process.Close();
                return DaemonProcessState.Successful;
            }
            catch (Exception)
            {
                return DaemonProcessState.Unsuccessful;
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
        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     ProcessOnExited event                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            //restart the process if _restarts < MaxRestarts or MaxRestarts = -1
            if (_restarts < _daemon.MaxRestarts || _daemon.MaxRestarts == -1)
            {
                Thread.Sleep(_daemon.ProcessRestartDelay);
                StartProcess();
                _restarts++;
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                         Other functions                                              //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

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

        public bool IsRunning()
        {
            if (_process == null)
                throw new ArgumentNullException("_process");

            try
            {
                foreach (Process proc in Process.GetProcesses())
                {
                    if (proc.Id == _process.Id)
                        return true;
                }
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        public Daemon GetDaemon => _daemon;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             Dispose                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Dispose

        private bool _disposed = false;

        //Public implementation of Dispose pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _process?.Dispose();
            }

            // Free any unmanaged objects here.
            _disposed = true;
        }

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              Other                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public enum DaemonProcessState
        {
            AlreadyStopped,
            AlreadyStarted,
            Successful,
            Unsuccessful,
        }
    }
}
