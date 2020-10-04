using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DaemonMaster.Core;
using DaemonMaster.Core.Jobs;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Core.Win32.PInvoke.Kernel32;
using DaemonMaster.Core.Win32.PInvoke.Userenv;
using Microsoft.Win32.SafeHandles;
using NLog;

namespace DaemonMasterService
{
    internal class DmProcess
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly DmServiceDefinition _serviceDefinition;
        private KillChildProcessJob _killChildProcessJob = new KillChildProcessJob();
        private Process _process;


        private uint _restarts;
        private DateTime _lastRestartTime;
        private bool _lastStartInUserSession = false;
        private string _lastSessionUsername = string.Empty;

        /// <summary>
        /// Get the process PID (if invalid it returns -1)
        /// </summary>
        internal uint ProcessPid
        {
            get
            {
                if (_process != null)
                    return (uint)_process.Id;

                return 0;
            }
        }

        /// <summary>
        /// Determines whether this instance is running.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">_process</exception>
        internal bool IsRunning()
        {
            if (_process == null)
                return false;

            try
            {
                return Process.GetProcesses().Any(proc => proc.Id == _process.Id);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Occurs when the max restarts are reached.
        /// </summary>
        internal event EventHandler MaxRestartsReached;

        /// <summary>
        /// Occurs when the pid of the process updates.
        /// </summary>
        internal event EventHandler<uint> UpdateProcessPid;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        Constructor + Init                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes a new instance of the <see cref="DmProcess"/> class.
        /// </summary>
        /// <param name="serviceDefinition">The service definition.</param>
        internal DmProcess(DmServiceDefinition serviceDefinition)
        {
            _serviceDefinition = serviceDefinition;

            //If it's a shortcut get the real binary path and arguments
            if (ShellLinkWrapper.IsShortcut(serviceDefinition.BinaryPath))
            {
                using (var shellLinkWrapper = new ShellLinkWrapper(serviceDefinition.BinaryPath))
                {
                    _serviceDefinition.BinaryPath = shellLinkWrapper.FilePath;
                    _serviceDefinition.Arguments = FormattingAndJoinArguments(shellLinkWrapper.Arguments, serviceDefinition.Arguments);
                }
            }

            ResetRestartCounter();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                  Start, Stop, Pause, Resume, etc                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////


        #region Start/Stop/Pause/Resume/etc

        /// <summary>
        /// Start the process.
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        internal void StartProcess(string username)
        {
            //CloseProcess old instances
            StopProcess();

            var startupInfo = new Kernel32.StartupInfo();
            var processInformation = new Kernel32.ProcessInformation();
            IntPtr environment = IntPtr.Zero;


            //Flags that specify the priority and creation method of the process
            Kernel32.CreationFlags creationFlags = Kernel32.CreationFlags.CreateUnicodeEnvironment | //Windows 7+ always using the unicode environment 
                                                   Kernel32.CreationFlags.CreateNewConsole |
                                                   _serviceDefinition.ProcessPriority.ConvertToCreationFlag();

            //Set the startupinfo
            startupInfo.cb = (uint)Marshal.SizeOf(startupInfo);
            startupInfo.desktop = "winsta0\\default";

            //Create command line arguments
            var cmdLine = "";
            if (string.Equals(Path.GetExtension(_serviceDefinition.BinaryPath), ".bat", StringComparison.OrdinalIgnoreCase))
                cmdLine = "cmd.exe /c";

            cmdLine += BuildDoubleQuotedString(_serviceDefinition.BinaryPath);

            if (!string.IsNullOrWhiteSpace(_serviceDefinition.Arguments))
                cmdLine += " " + _serviceDefinition.Arguments;

            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    //Set last start in user session
                    _lastStartInUserSession = false;
                    _lastSessionUsername = string.Empty;

                    if (!Kernel32.CreateProcess(
                        null,
                        cmdLine,
                        null,
                        null,
                        false,
                        creationFlags,
                        IntPtr.Zero, //TODO: Change
                        Path.GetDirectoryName(_serviceDefinition.BinaryPath),
                        ref startupInfo,
                        out processInformation))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    //Set last start in user session
                    _lastStartInUserSession = true;
                    _lastSessionUsername = username;

                    //Get user token
                    using (SafeAccessTokenHandle token = TokenHelper.GetPrimaryTokenByUsername(username))
                    {
                        if (token == null)
                            throw new Exception("GetPrimaryTokenByUsername: No valid session found.");

                        //Create environment block
                        if (!Userenv.CreateEnvironmentBlock(ref environment, token, false))
                            throw new Exception("StartInUserSession: CreateEnvironmentBlock failed.");

                        WindowsIdentity.RunImpersonated(token, () =>
                        {
                            if (!Advapi32.CreateProcessAsUser(
                                token,
                                null,
                                cmdLine,
                                null,
                                null,
                                false,
                                creationFlags,
                                environment,
                                Path.GetDirectoryName(_serviceDefinition.BinaryPath),
                                ref startupInfo,
                                out processInformation))
                            {
                                throw new Win32Exception(Marshal.GetLastWin32Error());
                            }
                        });
                    }
                }
            }
            finally
            {
                if (environment != IntPtr.Zero)
                    Userenv.DestroyEnvironmentBlock(environment);

                if (processInformation.processHandle != IntPtr.Zero)
                    Kernel32.CloseHandle(processInformation.processHandle);

                if (processInformation.threadHandle != IntPtr.Zero)
                    Kernel32.CloseHandle(processInformation.threadHandle);
            }

            //Get process and enable raising events
            _process = Process.GetProcessById((int)processInformation.processId);
            _process.EnableRaisingEvents = true;
            _process.Exited += ProcessOnExited;

            //Assign process to job
            if (KillChildProcessJob.IsSupportedWindowsVersion)
                _killChildProcessJob.AssignProcess(_process);

            _lastRestartTime = DateTime.UtcNow;

            //Trigger event to update pid
            OnUpdateProcessPid(processInformation.processId);
        }

        /// <summary>
        /// Stops the process.
        /// </summary>
        /// <returns></returns>
        internal bool StopProcess()
        {
            //TODO: Make better system
            Logger.Info("Stopping process...");

            //If process already stopped return
            if (!IsRunning())
            {
                CloseProcess();
                return true;
            }

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
                    //If console app then send Ctrl-C or Ctrl-Break command or kill when nothing works
                    if (_serviceDefinition.IsConsoleApplication)
                    {
                        if (!CloseConsoleApplication(_serviceDefinition.UseCtrlC))
                        {
                            Logger.Warn("CloseProcess console application is failed, killing process now...");
                            _process.Kill();
                        }
                    }
                    else
                    {
                        _process.Kill();
                    }
                }

                //Wait for a defined time
                if (!_process.WaitForExit(_serviceDefinition.ProcessTimeoutTime))
                {
                    Logger.Warn("Failed to stop process: timeout ({0}ms)", _serviceDefinition.ProcessTimeoutTime);
                    return false;
                }

                CloseProcess();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Kills the process.
        /// </summary>
        /// <returns></returns>
        internal void KillProcess()
        {
            Logger.Info("Killing process...");

            //If process already stopped return
            if (!IsRunning())
            {
                CloseProcess();
                return;
            }

            //Disable raising events (disable auto restart)
            _process.EnableRaisingEvents = false;
            _process.Kill();
            CloseProcess();
        }

        /// <summary>
        /// Resets the restart counter.
        /// </summary>
        internal void ResetRestartCounter()
        {
            _restarts = 0;
            _lastRestartTime = DateTime.UtcNow;
            Logger.Info("Cleared the restart counter.");
        }
        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                     ProcessOnExited event                                            //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////


        private void ProcessOnExited(object sender, EventArgs eventArgs)
        {
            if (_serviceDefinition.CounterResetTime != 0)
            {
                //Reset the counter if secondsBetweenCrashes is greater than or equal to CounterResetTime 
                try
                {
                    uint secondsBetweenCrashes = Convert.ToUInt32(DateTime.UtcNow.Subtract(_lastRestartTime).TotalSeconds);
                    if (secondsBetweenCrashes >= _serviceDefinition.CounterResetTime)
                    {
                        ResetRestartCounter();
                    }
                }
                //Reset the counter if an overflow happens because secondsBetweenCrashes must be greater than CounterResetTime
                catch (OverflowException)
                {
                    ResetRestartCounter();
                }
            }

            //Restart the process if _restarts < MaxRestarts or MaxRestarts = -1
            if (_restarts < _serviceDefinition.ProcessMaxRestarts || _serviceDefinition.ProcessMaxRestarts == -1)
            {
                Logger.Info("Restart process...");
                Thread.Sleep(_serviceDefinition.ProcessRestartDelay);

                StartProcess(_lastStartInUserSession ? _lastSessionUsername : null);

                _restarts++;
                _lastRestartTime = DateTime.UtcNow;
                Logger.Info("Current restart count is: {0}", _restarts);
                return;
            }

            Logger.Warn("Max restarts has been reached: {0}", _restarts);
            OnMaxRestartsReached();
        }

        protected virtual void OnMaxRestartsReached()
        {
            MaxRestartsReached?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnUpdateProcessPid(uint? pid = null)
        {
            UpdateProcessPid?.Invoke(this, pid ?? ProcessPid);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                         Other functions                                              //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes the console application.
        /// </summary>
        /// <param name="useCtrlC">if set to <c>true</c> [use control c].</param>
        /// <returns></returns>
        private bool CloseConsoleApplication(bool useCtrlC)
        {
            return _process == null || Kernel32.GenerateConsoleCtrlEvent(useCtrlC ? Kernel32.CtrlEvent.CtrlC : Kernel32.CtrlEvent.CtrlBreak, (uint)_process.Id);
        }

        /// <summary>
        /// Formatting and join arguments.
        /// </summary>
        /// <param name="shortcutArgs">The shortcut arguments.</param>
        /// <param name="userArgs">The user arguments.</param>
        /// <returns></returns>
        private static string FormattingAndJoinArguments(string shortcutArgs, string userArgs)
        {
            //Remove leading and trailing white-space characters
            shortcutArgs = shortcutArgs.Trim();
            userArgs = userArgs.Trim();

            //Combine strings with space
            string args = string.Concat(shortcutArgs, " ", userArgs);

            //Remove double spaces etc
            return Regex.Replace(args, @"\s+", " ");
        }

        ///<summary>
        /// Check if the string is quoted if not, it do it here
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static StringBuilder BuildCommandLineString(string filePath, string arguments)
        {
            var stringBuilder = new StringBuilder();
            filePath = filePath.Trim();

            bool filePathIsQuoted = filePath.StartsWith("\"", StringComparison.Ordinal) && filePath.EndsWith("\"", StringComparison.Ordinal);
            if (!filePathIsQuoted)
                stringBuilder.Append("\"");

            stringBuilder.Append(filePath);

            if (!filePathIsQuoted)
                stringBuilder.Append("\"");

            //Adds arguments to the StringBuilder
            if (!string.IsNullOrEmpty(arguments))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(arguments);
            }

            return stringBuilder;
        }

        /// <summary>
        /// Builds the double quoted string.
        /// </summary>
        /// <param name="s">The file path.</param>
        /// <returns></returns>
        private static StringBuilder BuildDoubleQuotedString(string s)
        {
            var stringBuilder = new StringBuilder();
            s = s.Trim();

            bool filePathIsQuoted = s.StartsWith("\"", StringComparison.Ordinal) && s.EndsWith("\"", StringComparison.Ordinal);
            if (!filePathIsQuoted)
                stringBuilder.Append("\"");

            stringBuilder.Append(s);

            if (!filePathIsQuoted)
                stringBuilder.Append("\"");

            return stringBuilder;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                               Dispose                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////


        #region Dispose

        public void CloseProcess()
        {
            if (_process != null)
            {
                _process.EnableRaisingEvents = false;
                _process.Dispose();
                _process = null;
            }

            OnUpdateProcessPid();
        }

        //Public implementation of Dispose pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseProcess();

                _killChildProcessJob?.Dispose();
                _killChildProcessJob = null;

                OnUpdateProcessPid();
            }

            // Free any unmanaged objects here.
        }

        #endregion
    }

}
