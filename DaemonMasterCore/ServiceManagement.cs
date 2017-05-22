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
using System.Windows.Documents;
using Microsoft.Win32;

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

        public static void CreateInteractiveService(Daemon daemon)
        {
            if (!Directory.Exists(DaemonMasterServicePath) || !File.Exists(DaemonMasterServicePath + DaemonMasterServiceFile))
                throw new IOException("Can't find the DaemonMasterService file!");

            IntPtr scManager = ADVAPI.OpenSCManager(null, null, (uint)ADVAPI.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE);

            if (scManager == IntPtr.Zero)
                throw new Win32Exception("Cannot open the service manager!, error:\n" + Marshal.GetLastWin32Error());

            IntPtr svManager = ADVAPI.CreateService(
                                                        scManager,
                                                        daemon.ServiceName,
                                                        daemon.DisplayName,
                                                        (uint)ADVAPI.SERVICE_ACCESS.SERVICE_ALL_ACCESS,
                                                        (uint)ADVAPI.SERVICE_TYPE.SERVICE_INTERACTIVE_PROCESS | (uint)ADVAPI.SERVICE_TYPE.SERVICE_WIN32_OWN_PROCESS,
                                                        (uint)ADVAPI.SERVICE_START.SERVICE_AUTO_START,
                                                        (uint)ADVAPI.SERVICE_ERROR_CONTROLE.SERVICE_ERROR_IGNORE,
                                                        DaemonMasterServicePath + DaemonMasterServiceFile + DaemonMasterServiceParameter,
                                                        null,
                                                        null,
                                                        "UI0Detect",
                                                        null,
                                                        null);
            if (svManager == IntPtr.Zero)
            {
                ADVAPI.CloseServiceHandle(scManager);
                throw new Win32Exception("Cannot create the service!, error:\n" + Marshal.GetLastWin32Error());
            }


            try
            {
                //Create an struct with the description of the service
                ADVAPI.SERVICE_DESCRIPTION serviceDescription;
                serviceDescription.lpDescription = daemon.Description;

                //Set the description of the service
                if (!ADVAPI.ChangeServiceConfig2(svManager, (uint)ADVAPI.DW_INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION, ref serviceDescription))
                    throw new Win32Exception("Cannot set the description of the service!, error:\n" + Marshal.GetLastWin32Error());
            }
            finally
            {
                ADVAPI.CloseServiceHandle(svManager);
                ADVAPI.CloseServiceHandle(scManager);
            }
        }

        public static int StartService(string serviceName)
        {
            try
            {
                if (!CheckUI0DetectService())
                    return -1;

                using (ServiceController scManager = new ServiceController(serviceName))
                {
                    if (scManager.Status == ServiceControllerStatus.Running)
                        return 0;

                    //Startet den Service
                    if (scManager.Status != ServiceControllerStatus.StartPending)
                        scManager.Start();

                    //Prüft ob der Service gestartet ist oder einen Timeout gemacht hat
                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(WaitForStatusTimeout));

                    return 1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static int StopService(string serviceName)
        {
            try
            {
                using (ServiceController scManager = new ServiceController(serviceName))
                {

                    if (scManager.Status == ServiceControllerStatus.Stopped)
                        return 0;

                    //Stoppt den Service
                    if (scManager.Status != ServiceControllerStatus.StopPending)
                        scManager.Stop();

                    //Prüft ob der Service gestoppt ist oder einen Timeout gemacht hat
                    scManager.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(WaitForStatusTimeout));
                    return 1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static void DeleteService(string serviceName)
        {
            //Open the service manager
            IntPtr scManager = ADVAPI.OpenSCManager(null, null, (uint)ADVAPI.SCM_ACCESS.SC_MANAGER_CONNECT);

            //Check if scManager is a valid pointer
            if (scManager == IntPtr.Zero)
                throw new Win32Exception("Cannot open the service manager!, error:\n" + Marshal.GetLastWin32Error());

            //Open the service
            IntPtr svManager = ADVAPI.OpenService(scManager, serviceName, (uint)ADVAPI.SERVICE_ACCESS.DELETE | (uint)ADVAPI.SERVICE_ACCESS.SERVICE_QUERY_STATUS | (uint)ADVAPI.SERVICE_ACCESS.SERVICE_ENUMERATE_DEPENDENTS);

            //Check if svManager is a valid pointer
            if (svManager == IntPtr.Zero)
            {
                ADVAPI.CloseServiceHandle(scManager);
                throw new Win32Exception("Cannot create the service!, error:\n" + Marshal.GetLastWin32Error());
            }

            try
            {
                //Check if the service has been stopped
                if (DaemonMasterUtils.QueryServiceStatusEx(svManager).currentState != (int)ADVAPI.SERVICE_STATE.SERVICE_STOPPED)
                {
                    if (StopService(serviceName) < 0)
                        throw new Win32Exception("Cannot stop the service!, error:\n" + Marshal.GetLastWin32Error());
                }

                //Delete the service
                if (!ADVAPI.DeleteService(svManager))
                    throw new Win32Exception("Cannot delete the service!, error:\n" + Marshal.GetLastWin32Error());
            }
            finally
            {
                ADVAPI.CloseServiceHandle(svManager);
                ADVAPI.CloseServiceHandle(scManager);
            }
        }

        public static bool ChangeServiceConfig2(string serviceName, string description)
        {
            //Open Sc Manager
            IntPtr scManager = ADVAPI.OpenSCManager(null, null, (uint)ADVAPI.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE);

            //Check if the scManager is not zero
            if (scManager == IntPtr.Zero)
                throw new Win32Exception("Cannot open the service Manager!, error:\n" + Marshal.GetLastWin32Error());

            //Open the service manager
            IntPtr svManager = ADVAPI.OpenService(scManager, serviceName,
                (uint)ADVAPI.SERVICE_ACCESS.SERVICE_QUERY_STATUS |
                (uint)ADVAPI.SERVICE_ACCESS.SERVICE_CHANGE_CONFIG |
                (uint)ADVAPI.SERVICE_ACCESS.SERVICE_QUERY_CONFIG);

            if (svManager == IntPtr.Zero)
            {
                ADVAPI.CloseServiceHandle(scManager);
                throw new Win32Exception("Cannot open the service!, error:\n" + Marshal.GetLastWin32Error());
            }

            try
            {
                //Query status of the service
                if (DaemonMasterUtils.QueryServiceStatusEx(svManager).currentState != (int)ADVAPI.SERVICE_STATE.SERVICE_STOPPED)
                {
                    return false;
                }

                //create an struct with description of the service
                ADVAPI.SERVICE_DESCRIPTION serviceDescription;
                serviceDescription.lpDescription = description;

                //Set the description of the service
                if (!ADVAPI.ChangeServiceConfig2(svManager, (uint)ADVAPI.DW_INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION, ref serviceDescription))
                    throw new Win32Exception("Cannot set the description of the service!, error:\n" + Marshal.GetLastWin32Error());

                return true;
            }
            finally
            {
                ADVAPI.CloseServiceHandle(svManager);
                ADVAPI.CloseServiceHandle(scManager);
            }
        }



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

        #endregion
    }
}
