///////////////////////////////////////////////////////////////////////////////////////////
////  DaemonMaster: DaemonProcess
////  
////  This file is part of DeamonMaster.
//// 
////  DeamonMaster is free software: you can redistribute it and/or modify
////  it under the terms of the GNU General Public License as published by
////  the Free Software Foundation, either version 3 of the License, or
////  (at your option) any later version.
////
////   DeamonMaster is distributed in the hope that it will be useful,
////   but WITHOUT ANY WARRANTY; without even the implied warranty of
////   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
////   GNU General Public License for more details.
////
////   You should have received a copy of the GNU General Public License
////   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
///////////////////////////////////////////////////////////////////////////////////////////

//using System;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Text;
//using DaemonMasterCore.Win32;
//using DaemonMasterCore.Win32.PInvoke;
//using Microsoft.Win32.SafeHandles;

//namespace DaemonMasterCore
//{
//    public sealed class BetterDaemonProcess : Process
//    {
//        bool disposed;
//        static object s_CreateProcessLock = new object();



//        public int GetProcessPid
//        {
//            get
//            {
//                try
//                {
//                    return Id;
//                }
//                catch (Exception)
//                {
//                    return 0;
//                }
//            }
//        }



//        public BetterDaemonProcess()
//        {
//            Exited += BetterDaemonProcess_Exited;
//        }

//        /// <summary>
//        /// Start the process in an user session (normaly used from a service)
//        /// </summary>
//        /// <returns></returns>
//        public bool StartInActiveUserSession()
//        {
//            Close();
//            ProcessStartInfo startInfo = StartInfo;

//            if (startInfo.FileName.Length == 0)
//                throw new InvalidOperationException("File name is missing!");

//            return StartInActiveUserSession(StartInfo);
//        }

//        private bool StartInActiveUserSession(ProcessStartInfo startInfo)
//        {
//            if (disposed)
//                throw new ObjectDisposedException(GetType().Name);

//            SafeProcessHandle processHandle = null;
//            ThreadHandle threadHandle = new ThreadHandle();

//            StringBuilder commandLine = BuildCommandLineString(startInfo.FileName, startInfo.Arguments);

//            NativeMethods.STARTUPINFO startupInfo = new NativeMethods.STARTUPINFO();
//            NativeMethods.PROCESS_INFORMATION processInformation = new NativeMethods.PROCESS_INFORMATION();


//            try
//            {
//                //Setup the creationFlags
//                int creationFlags = NativeMethods.CREATE_NEW_CONSOLE |
//                                    (int)NativeMethods.PRIORITY_CLASS.NORMAL_PRIORITY_CLASS |
//                                    NativeMethods.CREATE_UNICODE_ENVIRONMENT;

//                if (startInfo.CreateNoWindow) creationFlags |= NativeMethods.CREATE_NO_WINDOW;

//                //Read / setup the working dir 
//                string workingDir = startInfo.WorkingDirectory;
//                if (workingDir == String.Empty)
//                    workingDir = Environment.CurrentDirectory;

//                //Setup the environment
//                // set up the environment block parameter

//                //Get the active session id
//                int currentUserSessionId = NativeMethods.WTSGetActiveConsoleSessionId();

//                //Get user token
//                using (TokenHandle currentUserToken = TokenHandle.GetTokenFromSessionID(currentUserSessionId))
//                {
//                    NativeMethods.CreateProcessAsUser(
//                        currentUserToken,
//                        null,
//                        commandLine,
//                        null,
//                        null,
//                        false,
//                        creationFlags,
//                        null,
//                        workingDir,
//                        ref startupInfo,
//                        out processInformation);

//                    if (processInformation.hProcess != IntPtr.Zero &&
//                        processInformation.hProcess != NativeMethods.INVALID_HANDLE_VALUE)
//                        processHandle = new SafeProcessHandle(processInformation.hProcess, true);

//                    if (processInformation.hThread != IntPtr.Zero &&
//                        processInformation.hThread != NativeMethods.INVALID_HANDLE_VALUE)
//                        threadHandle.SetHandle(processInformation.hProcess);
//                }
//            }
//            finally
//            {
//                ;
//            }

//            if (!processHandle.IsInvalid)
//            {
//                (procSH);
//                return true;
//            }

//        }

//        /// <summary>
//        /// Check if the string is quoted if not, it do it here
//        /// </summary>
//        /// <param name="filePath"></param>
//        /// <param name="arguments"></param>
//        /// <returns></returns>
//        private static StringBuilder BuildCommandLineString(string filePath, string arguments)
//        {
//            StringBuilder stringBuilder = new StringBuilder();
//            filePath = filePath.Trim();

//            bool filePathIsQuoted = filePath.StartsWith("\"", StringComparison.Ordinal) && filePath.EndsWith("\"", StringComparison.Ordinal);
//            if (!filePathIsQuoted)
//                stringBuilder.Append("\"");

//            stringBuilder.Append(filePath);

//            if (!filePathIsQuoted)
//                stringBuilder.Append("\"");

//            //Adds arguments to the StringBuilder
//            if (!String.IsNullOrEmpty(arguments))
//            {
//                stringBuilder.Append(" ");
//                stringBuilder.Append(arguments);
//            }

//            return stringBuilder;
//        }

//        private void CreatePipe(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
//        {
//            NativeMethods.SECURITY_ATTRIBUTES securityAttributesParent =
//                new NativeMethods.SECURITY_ATTRIBUTES { bInheritHandle = true };

//            SafeFileHandle hTmp = null;
//            try
//            {
//                if (parentInputs)
//                {
//                    CreatePipeWithSecurityAttributes(out childHandle, out hTmp, securityAttributesParent, 0);
//                }
//                else
//                {
//                    CreatePipeWithSecurityAttributes(out hTmp,
//                                                          out childHandle,
//                                                          securityAttributesParent,
//                                                          0);
//                }

//                // Duplicate the parent handle to be non-inheritable so that the child process 
//                // doesn't have access. This is done for correctness sake, exact reason is unclear.
//                // One potential theory is that child process can do something brain dead like 
//                // closing the parent end of the pipe and there by getting into a blocking situation
//                // as parent will not be draining the pipe at the other end anymore. 
//                if (!NativeMethods.DuplicateHandle(new HandleRef(this, NativeMethods.GetCurrentProcess()),
//                                                                   hTmp,
//                                                                   new HandleRef(this, NativeMethods.GetCurrentProcess()),
//                                                                   out parentHandle,
//                                                                   0,
//                                                                   false,
//                                                                   NativeMethods.DUPLICATE_SAME_ACCESS))
//                {
//                    throw new Win32Exception();
//                }
//            }
//            finally
//            {
//                if (hTmp != null && !hTmp.IsInvalid)
//                {
//                    hTmp.Close();
//                }
//            }
//        }

//        private static void CreatePipeWithSecurityAttributes(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, NativeMethods.SECURITY_ATTRIBUTES lpPipeAttributes, int nSize)
//        {
//            bool ret = NativeMethods.CreatePipe(out hReadPipe, out hWritePipe, lpPipeAttributes, nSize);
//            if (!ret || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
//            {
//                throw new Win32Exception();
//            }
//        }



//        //////////////////////////////////////////////////////////////////////////////////////////////////////////
//        //                                     ProcessOnExited event                                            //
//        //////////////////////////////////////////////////////////////////////////////////////////////////////////

//        private void BetterDaemonProcess_Exited(object sender, EventArgs e)
//        {

//        }

//        //////////////////////////////////////////////////////////////////////////////////////////////////////////
//        //                                             Dispose                                                  //
//        //////////////////////////////////////////////////////////////////////////////////////////////////////////

//        protected override void Dispose(bool disposing)
//        {
//            if (disposed)
//                return;


//            if (disposing)
//            {
//                Exited -= BetterDaemonProcess_Exited;
//            }

//            disposed = true;
//            base.Dispose(disposing);
//        }

//    }
//}
