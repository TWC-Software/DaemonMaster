/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: DaemonProcess
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
using System.DirectoryServices.AccountManagement;
using System.Security.Authentication;
using System.Threading;
using DaemonMasterCore.Win32.PInvoke;
using NLog;

namespace DaemonMasterCore
{
    internal class DaemonProcess : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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

        private void Init()
        {
            //Create the start info for the process
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = _daemon.FullPath,
                Arguments = _daemon.Parameter,
                UseShellExecute = false //For .ink              
            };

            if (!_daemon.UseLocalSystem)
            {
                if (!String.IsNullOrWhiteSpace(_daemon.Username) && _daemon.Password != null)
                    throw new InvalidCredentialException();

                if (SystemManagement.ValidateUserWin32(_daemon.Username, _daemon.Password))
                {
                    startInfo.UserName = _daemon.Username;
                    startInfo.Password = _daemon.Password;
                }
                else
                {
                    throw new InvalidCredentialException();
                }
            }

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

        internal ProcessManagement.DaemonProcessState StartProcess()
        {
            if (_process.Start())
            {
                return ProcessManagement.DaemonProcessState.Successful;
            }

            return ProcessManagement.DaemonProcessState.Unsuccessful;
        }

        internal ProcessManagement.DaemonProcessState StopProcess()
        {
            //If process already stoppend return
            if (!IsRunning())
                return ProcessManagement.DaemonProcessState.AlreadyStopped;

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
                    return ProcessManagement.DaemonProcessState.Successful;
                }

                return ProcessManagement.DaemonProcessState.Unsuccessful;
            }
            catch (Exception)
            {
                return ProcessManagement.DaemonProcessState.Unsuccessful;
            }
        }

        internal ProcessManagement.DaemonProcessState KillProcess()
        {
            //If process already stoppend return
            if (!IsRunning())
                return ProcessManagement.DaemonProcessState.AlreadyStopped;

            //Disable raising events (disable auto restart)
            _process.EnableRaisingEvents = false;

            //Kill the process
            try
            {
                _process.Kill();
                _process.Close();
                return ProcessManagement.DaemonProcessState.Successful;
            }
            catch (Exception)
            {
                return ProcessManagement.DaemonProcessState.Unsuccessful;
            }
        }


        internal bool PauseProcess()
        {
            if (_process == null)
                throw new NullReferenceException();

            try
            {
                using (ThreadHandle threadHandle =
                    ThreadHandle.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, true, _process.Id))
                {
                    threadHandle.PauseThread();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal bool ResumeProcess()
        {
            if (_process == null)
                throw new NullReferenceException();
            try
            {
                using (ThreadHandle threadHandle =
                    ThreadHandle.OpenThread(NativeMethods.ThreadAccess.SUSPEND_RESUME, true, _process.Id))
                {
                    threadHandle.ResumeThread();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     ProcessOnExited event                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        internal void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            //restart the process if _restarts < MaxRestarts or MaxRestarts = -1
            if (_restarts < _daemon.MaxRestarts || _daemon.MaxRestarts == -1)
            {
                Thread.Sleep(_daemon.ProcessRestartDelay);
                StartProcess();
                _restarts++;
                _logger.Warn("Restart process... (restart: {0})", _restarts);
            }
            else
            {
                _logger.Warn("The maximum number of restarts has been reached!");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                         Other functions                                              //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        internal bool CloseConsoleApplication(bool useCtrlC)
        {
            if (_process == null)
                throw new NullReferenceException();


            if (useCtrlC)
            {
                return NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CtrlEvent.CTRL_C_EVENT, (uint)_process.Id);
            }
            else
            {
                return NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CtrlEvent.CTRL_BREAK_EVENT, (uint)_process.Id);
            }
        }

        internal bool IsRunning()
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

        internal Daemon GetDaemon => _daemon;


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
    }
}
