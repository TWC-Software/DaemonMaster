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
using DaemonMasterCore.Win32.PInvoke;
using NLog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading;

namespace DaemonMasterCore
{
    internal class DaemonProcess : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Daemon _daemon = null;
        private readonly Process _process = null;

        //Don't change!!
        private int _restarts = 0;
        private DateTime? lastRestartTime = null;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Constructor + Init                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor/Init

        public DaemonProcess(string serviceName, bool startInUserSessionAsService = false)
        {
            _daemon = RegistryManagement.LoadDaemonFromRegistry(serviceName);

            if (startInUserSessionAsService)
            {
                _process = ProcessManagement.StartProcessAsUser(_daemon.FullPath, _daemon.Parameter);
                InitProcessAsServiceInUserSession();
            }
            else
            {
                _process = new Process();
                InitProcessAsService();
            }
        }

        private void InitProcessAsService()
        {
            //Create the start info for the process
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = _daemon.FullPath,
                Arguments = _daemon.Parameter,
                UseShellExecute = false
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

        private void InitProcessAsServiceInUserSession()
        {
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
            if (!_process.HasExited || _process.Start())
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
                if (_process.MainWindowHandle != IntPtr.Zero)
                {
                    //Send close main window command
                    _process.CloseMainWindow();
                }
                else
                {
                    //If console app then send Ctrl-C or Ctrl-Break command
                    if (_daemon.ConsoleApplication)
                    {
                        CloseConsoleApplication(_daemon.UseCtrlC);
                    }
                    else
                    {

                        _process.Kill();
                        _process.Close();
                        return ProcessManagement.DaemonProcessState.Successful;
                    }
                }

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
            #region Counter reset system

            if (_daemon.CounterResetTime != 0 && lastRestartTime.HasValue)
            {
                //Reset the counter if secondsBetweenCraches is greater than or equal to CounterResetTime 
                try
                {
                    uint secondsBetweenCraches = Convert.ToUInt32(DateTime.UtcNow.Subtract(lastRestartTime.Value).TotalSeconds);
                    if (secondsBetweenCraches >= _daemon.CounterResetTime)
                    {
                        _restarts = 0;
                    }
                }
                //Reset the counter if an overflow happens because secondsBetweenCraches must be greater than CounterResetTime
                catch (OverflowException)
                {
                    _restarts = 0;
                }
            }
            #endregion

            //restart the process if _restarts < MaxRestarts or MaxRestarts = -1
            if (_restarts < _daemon.MaxRestarts || _daemon.MaxRestarts == -1)
            {
                Thread.Sleep(_daemon.ProcessRestartDelay);
                StartProcess();
                _restarts++;
                Logger.Warn("Restart process... (restart: {0})", _restarts);

                lastRestartTime = DateTime.UtcNow;
            }
            else
            {
                Logger.Warn("The maximum number of restarts has been reached!");
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
