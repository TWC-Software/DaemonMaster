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
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using DaemonMasterCore.Jobs;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
using NLog;

namespace DaemonMasterCore
{
    public class DaemonProcess : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private KillChildProcessJob killChildProcessJob = new KillChildProcessJob();

        private readonly ServiceStartInfo _serviceStartInfo;
        private readonly Process _process;

        //Don't change!!
        private int _restarts;
        private DateTime lastRestartTime;

        //Needed for shortcut support
        private string realPath = String.Empty;
        private string realArgs = String.Empty;

        /// <summary>
        /// Get the process PID (if invalid the property return -1)
        /// </summary>
        public int ProcessPID
        {
            get
            {
                try
                {
                    if (IsRunning())
                        return _process.Id;

                    return -1;
                }
                catch (Exception)
                {
                    return -1;
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Constructor + Init                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Constructor/Init

        public DaemonProcess(string serviceName, bool startInUserSessionAsService = false)
        {
            _serviceStartInfo = RegistryManagement.LoadServiceStartInfosFromRegistry(serviceName);

            //If it's a shortcut load the infos from them
            if (ShellLinkWrapper.IsShortcut(_serviceStartInfo.FullPath))
            {
                Logger.Info("Found shortcut, reading data...");

                using (ShellLinkWrapper shellLinkWrapper = new ShellLinkWrapper(_serviceStartInfo.FullPath))
                {
                    realPath = shellLinkWrapper.FilePath;
                    realArgs = FormattingAndJoinArguments(shellLinkWrapper.Arguments, _serviceStartInfo.Parameter);
                }
            }
            else
            {
                realPath = _serviceStartInfo.FullPath;
                realArgs = _serviceStartInfo.Parameter;
            }

            Logger.Info("File path:" + realPath);
            Logger.Info("Args:" + realArgs);

            if (startInUserSessionAsService)
            {
                _process = ProcessManagement.StartProcessAsUser(realPath, realArgs);
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
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = realPath,
                Arguments = realArgs,
                UseShellExecute = false

            };

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
            killChildProcessJob.AssignProcess(_process);
        }
        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                  Start, Stop, Pause, Resume, etc                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Start/Stop/Pause/Resume/etc

        public DaemonProcessState StartProcess()
        {
            if (IsRunning())
            {
                return DaemonProcessState.Successful;
            }

            _restarts = 0;
            lastRestartTime = DateTime.UtcNow;

            if (_process.Start())
            {
                killChildProcessJob.AssignProcess(_process);
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
                if (_process.MainWindowHandle != IntPtr.Zero)
                {
                    Logger.Warn("Send close main window command...");
                    //Send close main window command
                    _process.CloseMainWindow();
                }
                else
                {
                    //If console app then send Ctrl-C or Ctrl-Break command
                    if (_serviceStartInfo.ConsoleApplication)
                    {
                        Logger.Warn("Send Ctrl-C / Ctrl-Break command...");
                        CloseConsoleApplication(_serviceStartInfo.UseCtrlC);
                    }
                    else
                    {
                        Logger.Warn("The process has no GUI and is not a console app!\n => Killing the process...");
                        _process.Kill();
                        _process.Close();
                        return DaemonProcessState.Successful;
                    }
                }

                //Wait for a defined time
                if (_process.WaitForExit(_serviceStartInfo.ProcessKillTime))
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

        public bool KillProcess()
        {
            //If process already stoppend return
            if (!IsRunning())
                return true;

            //Disable raising events (disable auto restart)
            _process.EnableRaisingEvents = false;

            //Kill the process
            try
            {
                _process.Kill();
                _process.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public bool PauseProcess()
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

        public bool ResumeProcess()
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

        public void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            #region Counter reset system

            if (_serviceStartInfo.CounterResetTime != 0)
            {
                //Reset the counter if secondsBetweenCraches is greater than or equal to CounterResetTime 
                try
                {
                    uint secondsBetweenCrashes = Convert.ToUInt32(DateTime.UtcNow.Subtract(lastRestartTime).TotalSeconds);
                    if (secondsBetweenCrashes >= _serviceStartInfo.CounterResetTime)
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
            if (_restarts < _serviceStartInfo.MaxRestarts || _serviceStartInfo.MaxRestarts == -1)
            {
                Thread.Sleep(_serviceStartInfo.ProcessRestartDelay);
                _process.Close();
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


        public bool CloseConsoleApplication(bool useCtrlC)
        {
            if (_process == null)
                throw new NullReferenceException();


            if (useCtrlC)
            {
                return NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CtrlEvent.CTRL_C_EVENT, (uint)_process.Id);
            }
            return NativeMethods.GenerateConsoleCtrlEvent(NativeMethods.CtrlEvent.CTRL_BREAK_EVENT, (uint)_process.Id);
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
                return false;
            }
        }

        public ServiceStartInfo GetServiceStartInfo => _serviceStartInfo;

        private string FormattingAndJoinArguments(string shortcutArgs, string userArgs)
        {
            //Remove leading and trailing white-space characters
            shortcutArgs = shortcutArgs.Trim();
            userArgs = userArgs.Trim();

            //Combine strings with space
            string args = String.Concat(shortcutArgs, " ", userArgs);

            //Remove double spaces etc
            return Regex.Replace(args, @"\s+", " ");
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             Dispose                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Dispose

        private bool _disposed;

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
                killChildProcessJob?.Dispose();
            }

            // Free any unmanaged objects here.
            _disposed = true;
        }

        #endregion
    }
}
