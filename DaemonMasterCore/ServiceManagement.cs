/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceManagement
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using DaemonMasterCore.Config;
using DaemonMasterCore.Exceptions;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
using NLog;
using TimeoutException = System.ServiceProcess.TimeoutException;

namespace DaemonMasterCore
{

    public static class ServiceManagement
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        //Timeout Start/Stop Services (in ms)
        private const int WaitForStatusTimeout = 10000;

        private static readonly string DaemonMasterServicePath = AppDomain.CurrentDomain.BaseDirectory;
        private const string DaemonMasterServiceFile = "DaemonMasterService.exe";
        private const string DaemonMasterDevServiceFile = "DMService.exe";
        private const string DaemonMasterServiceParameter = " -service";


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Service                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Service
        /// <summary>
        /// Create an interactiv service under the "Local System" account with UI0Detect as dependencie
        /// </summary>
        /// <param name="serviceStartInfo"></param>
        public static void CreateInteractiveService(ServiceStartInfo serviceStartInfo)
        {
            if (!Directory.Exists(DaemonMasterServicePath) ||
                !File.Exists(DaemonMasterServicePath + DaemonMasterServiceFile))
                throw new IOException("Can't find the DaemonMasterService file!");

            using (ServiceControlManager scm =
                ServiceControlManager.Connect(NativeMethods.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE))
            {
                //Create the service type 
                uint serviceType = (uint)NativeMethods.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS;
                if (serviceStartInfo.CanInteractWithDesktop && serviceStartInfo.Username == null && DaemonMasterUtils.IsSupportedWindows10VersionOrLower()) //if the user is set to LocalSystem and the system version is lower than windows 10 1803
                {
                    serviceType |= (uint)NativeMethods.SERVICE_TYPE.SERVICE_INTERACTIVE_PROCESS;
                }

                using (ServiceHandle serviceHandle = scm.CreateService(
                    serviceStartInfo.ServiceName,
                    serviceStartInfo.DisplayName,
                    NativeMethods.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                    serviceType,
                    serviceStartInfo.StartType,
                    NativeMethods.SERVICE_ERROR_CONTROL.SERVICE_ERROR_NORMAL,
                    DaemonMasterServicePath + (ConfigManagement.LoadConfig().UseDevService ? DaemonMasterDevServiceFile : DaemonMasterServiceFile) + DaemonMasterServiceParameter,
                    null,
                    null,
                    ConvertDependenciesArraysToDoubleNullTerminatedString(serviceStartInfo.DependOnService, serviceStartInfo.DependOnGroup),
                    serviceStartInfo.Username,
                    serviceStartInfo.Password))
                {
                    serviceHandle.SetDescription(serviceStartInfo.Description);
                    serviceHandle.SetDelayedStart(serviceStartInfo.DelayedStart);
                }
            }
        }

        /// <summary>
        /// Start the service. Possible return values are AlreadyStarted, Successful
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="startProcessInUserSession">If true the service start the process in the current user session</param>
        /// <returns></returns>
        public static DaemonServiceState StartService(string serviceName, bool startProcessInUserSession = false)
        {
            using (ServiceController scManager = new ServiceController(serviceName))
            {
                //Create an list for the arguments
                List<string> args = new List<string>();

                if (scManager.Status == ServiceControllerStatus.Running)
                    return DaemonServiceState.AlreadyStarted;

                #region Arguments

                if (startProcessInUserSession)
                    args.Add("-startInUserSession");

                #endregion

                //Start the service
                if (scManager.Status != ServiceControllerStatus.StartPending)
                    scManager.Start(args.ToArray());

                try
                {
                    //Check if the service has been started or not
                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(WaitForStatusTimeout));
                }
                catch (TimeoutException)
                {
                    return DaemonServiceState.Unsuccessful;
                }

                return DaemonServiceState.Successful;
            }
        }

        /// <summary>
        /// Stop the service. Possible return values are AlreadyStopped, Successful
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static DaemonServiceState StopService(string serviceName)
        {

            using (ServiceController scManager = new ServiceController(serviceName))
            {
                if (scManager.Status == ServiceControllerStatus.Stopped)
                    return DaemonServiceState.AlreadyStopped;

                //Stoppt den Service
                if (scManager.Status != ServiceControllerStatus.StopPending)
                    scManager.Stop();


                try
                {
                    //Check if the service has been stopped or not
                    scManager.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(WaitForStatusTimeout));
                }
                catch (TimeoutException)
                {
                    return DaemonServiceState.Unsuccessful;
                }

                return DaemonServiceState.Successful;
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
        /// Kill the service if its running. Possible return values are AlreadyStopped, Successful, Unsuccessful (not in job object mode)
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="useJobObjectMethod"></param>
        public static DaemonServiceState KillService(string serviceName, bool useJobObjectMethod = false)
        {
            if (useJobObjectMethod)
            {

                int pid = GetServicePID(serviceName);

                if (pid != 0)
                {
                    Process.GetProcessById(pid).Kill();
                    return DaemonServiceState.Successful;
                }
                return DaemonServiceState.AlreadyStopped;
            }
            using (ServiceController serviceController = new ServiceController(serviceName))
            {
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                    return DaemonServiceState.AlreadyStopped;

                //Execute command to kill
                serviceController.ExecuteCommand((int)ServiceCommands.KillChildAndStop);

                try
                {
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(WaitForStatusTimeout));
                    return DaemonServiceState.Successful;
                }
                catch (TimeoutException)
                {
                    return DaemonServiceState.Unsuccessful;
                }
            }
        }

        /// <summary>
        /// Delete all stopped services 
        /// </summary>
        public static void DeleteAllServices()
        {
            foreach (var daemon in RegistryManagement.LoadDaemonItemsFromRegistry())
            {
                try
                {
                    _logger.Info("Delete '" + daemon.DisplayName + "'...");
                    DeleteService(daemon.ServiceName);
                    _logger.Info("Success");
                }
                catch (Exception e)
                {
                    _logger.Error("Failed to delete: " + daemon.DisplayName + "\n" + e.Message);
                }
            }
        }

        /// <summary>
        /// Kill all services
        /// </summary>
        public static void KillAllServices()
        {
            foreach (var daemon in RegistryManagement.LoadDaemonItemsFromRegistry())
            {
                try
                {
                    _logger.Info("Killing '" + daemon.DisplayName + "'...");
                    switch (KillService(daemon.ServiceName))
                    {
                        case DaemonServiceState.AlreadyStopped:
                            _logger.Warn("Already stopped");
                            break;

                        case DaemonServiceState.Successful:
                            _logger.Info("Success");
                            break;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Failed to kill: " + daemon.DisplayName + "\n" + e.Message);
                }
            }
        }

        /// <summary>
        /// Change the service config
        /// </summary>
        /// <param name="serviceStartInfo"></param>
        /// <returns></returns>
        public static void ChangeServiceConfig(ServiceStartInfo serviceStartInfo)
        {
            using (ServiceControlManager scm =
                ServiceControlManager.Connect(NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT))
            {
                using (ServiceHandle serviceHandle = scm.OpenService(serviceStartInfo.ServiceName, NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_STATUS | NativeMethods.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG | NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_CONFIG))
                {
                    NativeMethods.SERVICE_STATUS_PROCESS status = serviceHandle.QueryServiceStatusEx();

                    if (status.currentState != NativeMethods.SERVICE_STATE.SERVICE_STOPPED)
                        throw new ServiceNotStoppedException();

                    uint serviceType = (uint)NativeMethods.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS;
                    if (serviceStartInfo.CanInteractWithDesktop && serviceStartInfo.UseLocalSystem && DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
                    {
                        serviceType |= (uint)NativeMethods.SERVICE_TYPE.SERVICE_INTERACTIVE_PROCESS;
                    }

                    serviceHandle.ChangeConfig(serviceType, serviceStartInfo.StartType, serviceStartInfo.DisplayName, ConvertDependenciesArraysToDoubleNullTerminatedString(serviceStartInfo.DependOnService, serviceStartInfo.DependOnGroup), serviceStartInfo.Username, serviceStartInfo.Password);
                    serviceHandle.SetDescription(serviceStartInfo.Description);
                    serviceHandle.SetDelayedStart(serviceStartInfo.DelayedStart);
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
            using (ServiceController scManager = new ServiceController(serviceName))
            {
                if (scManager.Status == ServiceControllerStatus.Running)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Give the currennt status of the service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static ServiceControllerStatus GetServiceStatus(string serviceName)
        {
            using (ServiceController serviceController = new ServiceController(serviceName))
            {
                return serviceController.Status;
            }
        }

        /// <summary>
        /// Return the PID of the service
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns>Return the PID of the service</returns>
        public static int GetServicePID(string serviceName)
        {
            using (ServiceControlManager serviceControlManager = ServiceControlManager.Connect(NativeMethods.SCM_ACCESS.SC_MANAGER_CONNECT))
            {
                if (serviceControlManager.IsInvalid)
                    throw new Win32Exception("Can't connect to the service control manager!");

                using (ServiceHandle serviceHandle = serviceControlManager.OpenService(serviceName, NativeMethods.SERVICE_ACCESS.SERVICE_QUERY_STATUS))
                {
                    if (serviceHandle.IsInvalid)
                        throw new Win32Exception("The service handle is invalid!");

                    return serviceHandle.QueryServiceStatusEx().processID;
                }
            }
        }


        /// <summary>
        /// Join service dependencies and group dependencies string arrays and add double null termination to them
        /// </summary>
        /// <param name="stringServiceDependencies"></param>
        /// <param name="stringGroupDependencies"></param>
        /// <returns></returns>
        private static StringBuilder ConvertDependenciesArraysToDoubleNullTerminatedString(string[] stringServiceDependencies, string[] stringGroupDependencies)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string item in stringServiceDependencies)
            {
                stringBuilder.Append(item.Trim()).Append("\0");
            }

            foreach (string item in stringGroupDependencies)
            {
                stringBuilder.Append(NativeMethods.SC_GROUP_IDENTIFIER + item.Trim()).Append("\0");
            }
            stringBuilder.Append("\0");

            return stringBuilder;
        }


        #endregion
    }
}
