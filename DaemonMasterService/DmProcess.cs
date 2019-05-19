using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using DaemonMaster.Core;
using DaemonMaster.Core.Jobs;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Core.Win32.PInvoke.Kernel32;
using NLog;

namespace DaemonMasterService
{
    internal class DmProcess
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly KillChildProcessJob _killChildProcessJob = new KillChildProcessJob();
        private readonly DmServiceDefinition _serviceDefinition;
        private Process _process;


        private uint _restarts;
        private DateTime _lastRestartTime;
        private bool _lastStartInUserSession = false;

        /// <summary>
        /// Get the process PID (if invalid it returns -1)
        /// </summary>
        internal uint ProcessPid
        {
            get
            {
                try
                {
                    if (IsRunning())
                        return (uint)_process.Id;

                    return 0;
                }
                catch
                {
                    return 0;
                }
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
        internal event EventHandler UpdateProcessPid;

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
        /// Starts the in process in the service session.
        /// </summary>
        internal void StartInServiceSession()
        {
            Logger.Info("Start process in service session...");

            //Reset last start in user session
            _lastStartInUserSession = false;

            //Close old instances
            Close();

            //Create a new instance 
            _process = new Process();

            //Create the start info for the process
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false
            };

            //Support for .bat files
            try
            {
                if (Path.GetExtension(_serviceDefinition.BinaryPath) == ".bat")
                {
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/c " + BuildDoubleQuotedString(_serviceDefinition.BinaryPath) + _serviceDefinition.Arguments;
                    startInfo.WorkingDirectory = Path.GetDirectoryName(_serviceDefinition.BinaryPath) ?? throw new InvalidOperationException();
                }
                else
                {
                    startInfo.FileName = _serviceDefinition.BinaryPath;
                    startInfo.Arguments = _serviceDefinition.Arguments;
                }
            }
            catch
            {
                startInfo.FileName = _serviceDefinition.BinaryPath;
                startInfo.Arguments = _serviceDefinition.Arguments;
            }


            _process.StartInfo = startInfo;

            //Enable raising events for auto restart
            _process.EnableRaisingEvents = true;
            _process.Exited += ProcessOnExited;
            _process.Start();

            //Assign process to job
            if (KillChildProcessJob.IsSupportedWindowsVersion)
                _killChildProcessJob.AssignProcess(_process);

            //Set process priority
            _process.PriorityClass = _serviceDefinition.ProcessPriority;

            _lastRestartTime = DateTime.UtcNow;

            //Trigger event to update pid
            OnUpdateProcessPid();
        }

        /// <summary>
        /// Starts the process in the user session.
        /// </summary>
        /// <exception cref="Win32Exception"></exception>
        internal void StartInUserSession()
        {
            Logger.Info("Start process in user session...");

            //Set last start in user session
            _lastStartInUserSession = true;

            //Close old instances
            Close();

            //Set the startupinfo
            var startupInfo = new Advapi32.StartupInfo();
            startupInfo.cb = (uint)Marshal.SizeOf(startupInfo);


            var processInformation = new Advapi32.ProcessInformation();

            //Flags that specify the priority and creation method of the process
            Advapi32.CreationFlags creationFlags = Advapi32.CreationFlags.CreateUnicodeEnvironment | Advapi32.CreationFlags.CreateNewConsole; //Windows 7+ always using the unicode environment 

            //Process priority
            switch (_serviceDefinition.ProcessPriority)
            {
                case ProcessPriorityClass.Idle:
                    creationFlags |= Advapi32.CreationFlags.IdlePriorityClass;
                    break;
                case ProcessPriorityClass.High:
                    creationFlags |= Advapi32.CreationFlags.HighPriorityClass;
                    break;
                case ProcessPriorityClass.RealTime:
                    creationFlags |= Advapi32.CreationFlags.RealtimePriorityClass;
                    break;
                case ProcessPriorityClass.BelowNormal:
                    creationFlags |= Advapi32.CreationFlags.BelowNormalPriorityClass;
                    break;
                case ProcessPriorityClass.AboveNormal:
                    creationFlags |= Advapi32.CreationFlags.AboveNormalPriorityClass;
                    break;
                default:
                    creationFlags |= Advapi32.CreationFlags.NormalPriorityClass;
                    break;
            }


            //Create default process security attributes
            var processSecurityAttributes = new Kernel32.SecurityAttributes();
            processSecurityAttributes.length = (uint)Marshal.SizeOf(processSecurityAttributes);

            //Create default thread security attributes
            var threadSecurityAttributes = new Kernel32.SecurityAttributes();
            threadSecurityAttributes.length = (uint)Marshal.SizeOf(threadSecurityAttributes);


            //TODO: Change Kernel32.WTSGetActiveConsoleSessionId() to WTSEnumerateSessionsW for RDP
            //Get user token
            using (TokenHandle currentUserToken = TokenHandle.GetTokenFromSessionId(Kernel32.WTSGetActiveConsoleSessionId()))
            {
                try
                {
                    StringBuilder cmdLine;
                    if (Path.GetExtension(_serviceDefinition.BinaryPath) == ".bat")   //Cmd mode: (.bat)
                    {
                        cmdLine = new StringBuilder();
                        cmdLine.Append(BuildDoubleQuotedString("cmd.exe"));
                        cmdLine.Append(" /c ");
                        cmdLine.Append(BuildDoubleQuotedString(_serviceDefinition.BinaryPath));
                        cmdLine.Append(" ");
                        cmdLine.Append(_serviceDefinition.Arguments);
                    }
                    else //Normal mode (.exe, etc)
                    {
                        cmdLine = BuildCommandLineString(_serviceDefinition.BinaryPath, _serviceDefinition.Arguments);
                    }

                    if (!Advapi32.CreateProcessAsUser(
                        currentUserToken,
                        null,
                        cmdLine,
                        processSecurityAttributes,
                        threadSecurityAttributes,
                        false,
                        creationFlags,
                        IntPtr.Zero,
                        Path.GetDirectoryName(_serviceDefinition.BinaryPath),
                        ref startupInfo,
                        out processInformation))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    _process = Process.GetProcessById((int)processInformation.processId);

                    //Enable raising events
                    _process.EnableRaisingEvents = true;
                    _process.Exited += ProcessOnExited;

                    //Assign process to job
                    if (KillChildProcessJob.IsSupportedWindowsVersion)
                        _killChildProcessJob.AssignProcess(_process);

                    _lastRestartTime = DateTime.UtcNow;

                    //Trigger event to update pid
                    OnUpdateProcessPid();
                }
                finally
                {
                    if (processInformation.processHandle != IntPtr.Zero)
                        Kernel32.CloseHandle(processInformation.processHandle);

                    if (processInformation.threadHandle != IntPtr.Zero)
                        Kernel32.CloseHandle(processInformation.threadHandle);
                }
            }
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
                return true;

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
                            Logger.Warn("Close console application is failed, killing process now...");
                            _process.Kill();
                            return true;
                        }
                    }
                    else
                    {
                        _process.Kill();
                        return true;
                    }
                }

                //Wait for a defined time
                if (!_process.WaitForExit(_serviceDefinition.ProcessTimeoutTime))
                {
                    Logger.Warn("Failed to stop process: timeout ({0}ms)", _serviceDefinition.ProcessTimeoutTime);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _process.Kill();

                Logger.Error(ex, ex.Message);
                return false;
            }
            finally
            {
                Close();

                //Trigger event to update pid
                OnUpdateProcessPid();
            }
        }

        /// <summary>
        /// Kills the process.
        /// </summary>
        /// <returns></returns>
        internal bool KillProcess()
        {
            Logger.Info("Killing process...");
            //If process already stopped return
            if (!IsRunning())
                return true;

            //Disable raising events (disable auto restart)
            _process.EnableRaisingEvents = false;

            try
            {
                _process.Kill();
            }
            finally
            {
                Close();

                //Trigger event to update pid
                OnUpdateProcessPid();
            }

            return true;
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
            //Trigger event to update pid
            OnUpdateProcessPid();

            if (_serviceDefinition.CounterResetTime != 0)
            {
                //Reset the counter if secondsBetweenCraches is greater than or equal to CounterResetTime 
                try
                {
                    uint secondsBetweenCrashes = Convert.ToUInt32(DateTime.UtcNow.Subtract(_lastRestartTime).TotalSeconds);
                    if (secondsBetweenCrashes >= _serviceDefinition.CounterResetTime)
                    {
                        ResetRestartCounter();
                    }
                }
                //Reset the counter if an overflow happens because secondsBetweenCraches must be greater than CounterResetTime
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

                if (_lastStartInUserSession)
                {
                    StartInUserSession();
                }
                else
                {
                    StartInServiceSession();
                }

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

        protected virtual void OnUpdateProcessPid()
        {
            UpdateProcessPid?.Invoke(this, EventArgs.Empty);
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
            if (_process == null)
                return true;

            return Kernel32.GenerateConsoleCtrlEvent(useCtrlC ? Kernel32.CtrlEvent.CtrlC : Kernel32.CtrlEvent.CtrlBreak, (uint)_process.Id);
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

        public void Close()
        {
            _process?.Dispose();
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
                _process?.Dispose();
                _killChildProcessJob?.Dispose();
            }

            // Free any unmanaged objects here.
        }

        #endregion
    }

}
