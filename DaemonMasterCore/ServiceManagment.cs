/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SERVICE MANAGMENT FILE
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
using Microsoft.Win32;

namespace DaemonMasterCore
{

    public static class ServiceManagment
    {
        //Timeout Start/Stop Services (in ms)
        private const int timeout = 10000;

        private static string DaemonMasterServicePath = AppDomain.CurrentDomain.BaseDirectory;
        private const string DaemonMasterServiceFile = "DaemonMasterService.exe";
        private const string DaemonMasterServiceParameter = " -service";


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Service                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Service

        public static void CreateInteractiveService(Daemon daemon)
        {
            if (!Directory.Exists(DaemonMasterServicePath) || !File.Exists(DaemonMasterServicePath + DaemonMasterServiceFile))
                throw new Exception("Can't find the DaemonMasterService file!");

            IntPtr scManager = ADVAPI.OpenSCManager(null, null, (uint)ADVAPI.SCM_ACCESS.SC_MANAGER_CREATE_SERVICE);

            if (scManager != IntPtr.Zero)
            {
                try
                {
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
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        ADVAPI.CloseServiceHandle(svManager);
                    }
                }
                finally
                {
                    ADVAPI.CloseServiceHandle(scManager);
                }
            }
            else
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public static int StartService(Daemon daemon)
        {
            try
            {
                if (!CheckUI0DetectService())
                    return -1;

                using (ServiceController scManager = new ServiceController(daemon.ServiceName))
                {
                    if (scManager.Status == ServiceControllerStatus.Running)
                        return 0;

                    //Startet den Service
                    if (scManager.Status != ServiceControllerStatus.StartPending)
                        scManager.Start();

                    //Prüft ob der Service gestartet ist oder einen Timeout gemacht hat
                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout));

                    return 1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static int StopService(Daemon daemon)
        {
            try
            {
                using (ServiceController scManager = new ServiceController(daemon.ServiceName))
                {

                    if (scManager.Status == ServiceControllerStatus.Stopped)
                        return 0;

                    //Stoppt den Service
                    if (scManager.Status != ServiceControllerStatus.StopPending)
                        scManager.Stop();

                    //Prüft ob der Service gestoppt ist oder einen Timeout gemacht hat
                    scManager.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeout));

                    return 1;
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static void DeleteService(Daemon daemon)
        {

            IntPtr scManager = ADVAPI.OpenSCManager(null, null, (uint)ADVAPI.SCM_ACCESS.SC_MANAGER_CONNECT);

            if (scManager == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            IntPtr svManager = ADVAPI.OpenService(scManager, daemon.ServiceName, (uint)ADVAPI.SERVICE_ACCESS.DELETE | (uint)ADVAPI.SERVICE_ACCESS.SERVICE_QUERY_STATUS | (uint)ADVAPI.SERVICE_ACCESS.SERVICE_ENUMERATE_DEPENDENTS);

            if (svManager == IntPtr.Zero)
            {
                ADVAPI.CloseServiceHandle(scManager);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            //Prüft ob der Service gestoppt ist
            if (QueryServiceStatusEx(svManager).currentState != (int)ADVAPI.SERVICE_STATE.SERVICE_STOPPED)
            {
                if (StopService(daemon) < 0)
                {
                    ADVAPI.CloseServiceHandle(scManager);
                    ADVAPI.CloseServiceHandle(svManager);
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            //Löscht den Service
            if (!ADVAPI.DeleteService(svManager))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            ADVAPI.CloseServiceHandle(scManager);
            ADVAPI.CloseServiceHandle(svManager);
        }

        public static void DeleteAllServices(ObservableCollection<Daemon> daemons)
        {
            foreach (Daemon d in daemons)
            {
                try
                {
                    DeleteService(d);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }


        //http://www.pinvoke.net/default.aspx/advapi32.QueryServiceStatusEx
        public static ADVAPI.SERVICE_STATUS_PROCESS QueryServiceStatusEx(IntPtr svManager)
        {
            IntPtr buffer = IntPtr.Zero;
            int size = 0;

            try
            {
                ADVAPI.QueryServiceStatusEx(svManager, 0, buffer, size, out size);
                //Reserviere Speicher in der größe von size
                buffer = Marshal.AllocHGlobal(size);

                if (!ADVAPI.QueryServiceStatusEx(svManager, 0, buffer, size, out size))
                {

                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return (ADVAPI.SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(buffer, typeof(ADVAPI.SERVICE_STATUS_PROCESS));
            }
            catch (Exception)
            {
                throw new NotImplementedException();
            }
            finally
            {
                //Gebe Speicher, wenn genutzt, wieder frei
                if (!buffer.Equals(IntPtr.Zero))
                    Marshal.FreeHGlobal(buffer);
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

                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout));

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

        //Ändert den Regkey so das Interactive Services erlaubt werden (Set NoInteractiveServices to 0)
        public static bool ActivateInteractiveServices()
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", true))
                {
                    if (regKey != null)
                    {
                        if (regKey.GetValue("NoInteractiveServices").ToString() != "0")
                            regKey.SetValue("NoInteractiveServices", "0", RegistryValueKind.DWord);

                        regKey.Close();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Ändert den Regkey so das Interactive Services erlaubt werden (Set NoInteractiveServices to 0)
        public static bool CheckNoInteractiveServicesRegKey()
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", true))
                {
                    if (regKey != null)
                    {
                        if (regKey.GetValue("NoInteractiveServices").ToString() == "0")
                        {

                            regKey.Close();
                            return true;
                        }

                        return false;
                    }
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

    }
}
