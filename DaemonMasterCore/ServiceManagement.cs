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

using DaemonMasterCore.Exceptions;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
using System;
using System.IO;
using System.ServiceProcess;

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
            if (!Directory.Exists(DaemonMasterServicePath) ||
                !File.Exists(DaemonMasterServicePath + DaemonMasterServiceFile))
                throw new IOException("Can't find the DaemonMasterService file!");

            using (ServiceControlManager scm =
                ServiceControlManager.Connect(NativeMethods.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE))
            {
                using (ServiceHandle serviceHandle = scm.CreateService(
                    daemon.ServiceName,
                    daemon.DisplayName,
                    NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                    NativeMethods.SERVICE_TYPE.SERVICE_INTERACTIVE_PROCESS |
                    NativeMethods.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                    daemon.StartType,
                    NativeMethods.SERVICE_ERROR_CONTROL.SERVICE_ERROR_NORMAL,
                    DaemonMasterServicePath + DaemonMasterServiceFile + DaemonMasterServiceParameter,
                    null,
                    null,
                    "UI0Detect",
                    null,
                    null))
                {
                    serviceHandle.SetDescription(daemon.Description);
                    serviceHandle.SetDelayedStart(daemon.DelayedStart);
                }
            }
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
        /// Delete the service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static void DeleteService(string serviceName)
        {
            using (ServiceControlManager scm =
                ServiceControlManager.Connect(NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT))
            {
                using (ServiceHandle serviceHandle = scm.OpenService(serviceName, NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_STATUS | NativeMethods.SERVICE_ACCESS.DELETE))
                {
                    NativeMethods.SERVICE_STATUS_PROCESS status = serviceHandle.QueryServiceStatusEx();

                    if (status.currentState != NativeMethods.SERVICE_STATE.SERVICE_STOPPED)
                        throw new ServiceNotStoppedException();

                    serviceHandle.DeleteService();
                }
            }
        }

        /// <summary>
        /// Change the service config
        /// </summary>
        /// <param name="daemon"></param>
        /// <returns></returns>
        public static void ChangeServiceConfig(Daemon daemon)
        {
            using (ServiceControlManager scm =
                ServiceControlManager.Connect(NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT))
            {
                using (ServiceHandle serviceHandle = scm.OpenService(daemon.ServiceName, NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_STATUS | NativeMethods.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG | NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_CONFIG))
                {
                    NativeMethods.SERVICE_STATUS_PROCESS status = serviceHandle.QueryServiceStatusEx();

                    if (status.currentState != NativeMethods.SERVICE_STATE.SERVICE_STOPPED)
                        throw new ServiceNotStoppedException();

                    serviceHandle.ChangeConfig(daemon.StartType, daemon.DisplayName);
                    serviceHandle.SetDescription(daemon.Description);
                    serviceHandle.SetDelayedStart(daemon.DelayedStart);
                }
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
