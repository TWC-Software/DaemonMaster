/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SERVICE MANAGEMENT FILE
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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using DaemonMasterCore.Win32;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Windows.Documents;
using Microsoft.Win32;
using DaemonMasterCore.Exceptions;

namespace DaemonMasterCore
{

    public static class ServiceManagement
    {
        //Timeout Start/Stop Services (in ms)
        private const int WaitForStatusTimeout = 10000;

        private static readonly string DaemonMasterServicePath = AppDomain.CurrentDomain.BaseDirectory;
        private const string DaemonMasterServiceFile = "DaemonMasterService.exe";
        private const string DaemonMasterServiceParameter = " -service";


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Service                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Service
        /// <summary>
        /// Create an interactiv service under the "Local System" account with UI0Detect as dependencie
        /// </summary>
        /// <param name="daemon"></param>
        public static void CreateInteractiveService(Daemon daemon)
        {
            if (!Directory.Exists(DaemonMasterServicePath) || !File.Exists(DaemonMasterServicePath + DaemonMasterServiceFile))
                throw new IOException("Can't find the DaemonMasterService file!");

            IntPtr scManager = NativeMethods.OpenSCManager(null, null, (uint)NativeMethods.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE);

            if (scManager == IntPtr.Zero)
                throw new Win32Exception("Cannot open the service manager!, error:\n" + Marshal.GetLastWin32Error());

            IntPtr svManager = NativeMethods.CreateService(
                                                        scManager,
                                                        daemon.ServiceName,
                                                        daemon.DisplayName,
                                                        (uint)NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                                                        (uint)NativeMethods.SERVICE_TYPE.SERVICE_INTERACTIVE_PROCESS | (uint)NativeMethods.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                                                        (uint)NativeMethods.SERVICE_START.SERVICE_AUTO_START,
                                                        (uint)NativeMethods.SERVICE_ERROR_CONTROLE.SERVICE_ERROR_IGNORE,
                                                        DaemonMasterServicePath + DaemonMasterServiceFile + DaemonMasterServiceParameter,
                                                        null,
                                                        null,
                                                        "UI0Detect",
                                                        null,
                                                        null);
            if (svManager == IntPtr.Zero)
            {
                NativeMethods.CloseServiceHandle(scManager);
                throw new Win32Exception("Cannot create the service!, error:\n" + Marshal.GetLastWin32Error());
            }


            NativeMethods.CloseServiceHandle(svManager);
            NativeMethods.CloseServiceHandle(scManager);
        }

        /// <summary>
        /// Start the service. Possible return values are AlreadyStarted, Successful and Error
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static State StartService(string serviceName)
        {
            try
            {
                //if (!CheckUI0DetectService())
                //    return State.Unsuccessful;
                using (ServiceController scManager = new ServiceController(serviceName))
                {
                    if (scManager.Status == ServiceControllerStatus.Running)
                        return State.AlreadyStarted;

                    //Startet den Service
                    if (scManager.Status != ServiceControllerStatus.StartPending)
                        scManager.Start();

                    //Prüft ob der Service gestartet ist oder einen Timeout gemacht hat
                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(WaitForStatusTimeout));

                    return State.Successful;
                }
            }
            catch (Exception)
            {
                return State.Error;
            }
        }

        /// <summary>
        /// Stop the service. Possible return values are AlreadyStopped, Successful and Error
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static State StopService(string serviceName)
        {
            try
            {
                using (ServiceController scManager = new ServiceController(serviceName))
                {

                    if (scManager.Status == ServiceControllerStatus.Stopped)
                        return State.AlreadyStopped;

                    //Stoppt den Service
                    if (scManager.Status != ServiceControllerStatus.StopPending)
                        scManager.Stop();

                    //Prüft ob der Service gestoppt ist oder einen Timeout gemacht hat
                    scManager.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(WaitForStatusTimeout));
                    return State.Successful;
                }
            }
            catch (Exception)
            {
                return State.Error;
            }
        }

        /// <summary>
        /// Delete the service. Possible return values are NotStopped and Successful
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static State DeleteService(string serviceName)
        {
            //Open the service manager
            IntPtr scManager = NativeMethods.OpenSCManager(null, null, (uint)NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT);

            //Check if scManager is a valid pointer
            if (scManager == IntPtr.Zero)
                throw new Win32Exception("Cannot open the service manager!, error:\n" + Marshal.GetLastWin32Error());

            //Open the service
            IntPtr svManager = NativeMethods.OpenService(scManager, serviceName, (uint)NativeMethods.SERVICE_ACCESS.DELETE | (uint)NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_STATUS | (uint)NativeMethods.SERVICE_ACCESS.SERVICE_ENUMERATE_DEPENDENTS);

            //Check if svManager is a valid pointer
            if (svManager == IntPtr.Zero)
            {
                NativeMethods.CloseServiceHandle(scManager);
                throw new Win32Exception("Cannot open the service!, error:\n" + Marshal.GetLastWin32Error());
            }

            try
            {
                //Check if the service has been stopped
                if (DaemonMasterUtils.QueryServiceStatusEx(svManager).currentState != (int)NativeMethods.SERVICE_STATE.SERVICE_STOPPED)
                {
                    //if (StopService(serviceName) < 0)
                    //    throw new Win32Exception("Cannot stop the service!, error:\n" + Marshal.GetLastWin32Error());
                    return State.NotStopped;
                }

                //Delete the service
                if (!NativeMethods.DeleteService(svManager))
                    throw new Win32Exception("Cannot delete the service!, error:\n" + Marshal.GetLastWin32Error());

                return State.Successful;
            }
            finally
            {
                NativeMethods.CloseServiceHandle(svManager);
                NativeMethods.CloseServiceHandle(scManager);
            }
        }

        /// <summary>
        /// Change the service config with the handle (description). Possible return values are NotStopped and Successful
        /// </summary>
        /// <param name="svManager"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private static void ChangeServiceConfig2(IntPtr svManager, string description)
        {
            //Create an struct with description of the service
            NativeMethods.SERVICE_DESCRIPTION serviceDescription;
            serviceDescription.lpDescription = description;

            //Set the description of the service
            if (!NativeMethods.ChangeServiceConfig2(svManager, (uint)NativeMethods.DW_INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION, ref serviceDescription))
                throw new Win32Exception("Cannot set the description of the service!, error:\n" + Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Change the service config with the handle (delayed start). Possible return values are NotStopped and Successful
        /// </summary>
        /// <param name="svManager"></param>
        /// <param name="delayedStart"></param>
        /// <returns></returns>
        private static void ChangeServiceConfig2(IntPtr svManager, bool delayedStart)
        {
            // //Create an struct with description of the service
            NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO serviceDelayedStart;
            serviceDelayedStart.delayedStart = delayedStart;

            //Set the description of the service
            if (!NativeMethods.ChangeServiceConfig2(svManager, (uint)NativeMethods.DW_INFO_LEVEL.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                ref serviceDelayedStart))
                throw new Win32Exception("Cannot set the description of the service!, error:\n" +
                                         Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Change the service config. Possible return values are NotStopped and Successful
        /// </summary>
        /// <param name="daemon"></param>
        /// <returns></returns>
        public static State ChangeCompleteServiceConfig(Daemon daemon)
        {
            //Open Sc Manager
            IntPtr scManager = NativeMethods.OpenSCManager(null, null, (uint)NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT);

            //Check if the scManager is not zero
            if (scManager == IntPtr.Zero)
                throw new Win32Exception("Cannot open the service Manager!, error:\n" + Marshal.GetLastWin32Error());

            //Open the service manager
            IntPtr svManager = NativeMethods.OpenService(scManager, daemon.ServiceName,
                (uint)NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_STATUS |
                (uint)NativeMethods.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG);

            if (svManager == IntPtr.Zero)
            {
                NativeMethods.CloseServiceHandle(scManager);
                throw new Win32Exception("Cannot open the service!, error:\n" + Marshal.GetLastWin32Error());
            }

            //Query status of the service
            if (DaemonMasterUtils.QueryServiceStatusEx(svManager).currentState != (int)NativeMethods.SERVICE_STATE.SERVICE_STOPPED)
                return State.NotStopped;

            try
            {
                ChangeServiceConfig2(svManager, daemon.Description);
                ChangeServiceConfig2(svManager, daemon.DelayedStart);

                if (!NativeMethods.ChangeServiceConfig(svManager, NativeMethods.SERVICE_NO_CHANGE, (uint)daemon.StartType,
                    NativeMethods.SERVICE_NO_CHANGE, null, null, null, null/*String.Concat(daemon.DependOnService)*/, null, null, daemon.DisplayName))
                    throw new Win32Exception("Cannot set the config of the service!, error:\n" + Marshal.GetLastWin32Error());

                return State.Successful;
            }
            finally
            {
                NativeMethods.CloseServiceHandle(svManager);
                NativeMethods.CloseServiceHandle(scManager);
            }
        }

        /// <summary>
        /// Check if the service UI0Detect running
        /// </summary>
        /// <returns></returns>
        public static bool CheckUI0DetectService()
        {
            try
            {
                using (ServiceController scManager = new ServiceController("UI0Detect"))
                {
                    if (scManager.Status == ServiceControllerStatus.Running)
                        return true;

                    if (scManager.Status != ServiceControllerStatus.StartPending)
                        scManager.Start();

                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(WaitForStatusTimeout));

                    return scManager.Status == ServiceControllerStatus.Running;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the service running
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static bool IsServiceRunning(string serviceName)
        {
            try
            {
                using (ServiceController scManager = new ServiceController(serviceName))
                {
                    if (scManager.Status == ServiceControllerStatus.Running)
                        return true;

                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }



        #endregion

        [Flags]
        public enum State
        {
            NotStopped,
            NotStarted,
            AlreadyStopped,
            AlreadyStarted,
            Stopped,
            Running,
            Paused,
            Deleted,
            Successful,
            Unsuccessful,
            Error,
            ParametersArNotValid
        }
    }
}
