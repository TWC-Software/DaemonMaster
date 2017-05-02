/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: CORE FILE 
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


using DaemonMaster.Win32;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Media;
using DaemonMaster.Language;

namespace DaemonMaster.Core
{
    public static class DaemonMasterCore
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

                using (ServiceController scManager = new ServiceController("dm_" + daemon.ServiceName))
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
                using (ServiceController scManager = new ServiceController("dm_" + daemon.ServiceName))
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

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Security                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Security

        public static SecureString ConvertStringToSecureString(string value)
        {
            SecureString secString = new SecureString();

            if (value.Length > 0)
            {
                foreach (char c in value.ToCharArray())
                {
                    secString.AppendChar(c);
                }
                return secString;
            }
            return null;
        }

        public static String ConvertSecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             Session 0                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Session 0

        public static void SwitchToSession0()
        {
            if (CheckUI0DetectService())
            {
                MessageBoxResult result = MessageBox.Show(LanguageSystem.resManager.GetString("windows10_mouse_keyboard", LanguageSystem.culture), LanguageSystem.resManager.GetString("warning", LanguageSystem.culture), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    WINSTA.WinStationSwitchToServicesSession();
                }
            }
            else
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("failed_start_UI0detect_service", LanguageSystem.culture), LanguageSystem.resManager.GetString("error", LanguageSystem.culture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             JSON                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region JSON

        public static void ExportList(ObservableCollection<Daemon> daemons)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer);
            saveFileDialog.Filter = "JSON (*.json)|*.json|" +
                                    "All files (*.*)|*.*";
            saveFileDialog.DefaultExt = "json";
            saveFileDialog.AddExtension = true;

            //Wenn eine Datei gewählt worden ist
            if (saveFileDialog.ShowDialog() == true)
            {
                SaveDaemons(daemons, saveFileDialog.FileName);
            }
        }

        public static ObservableCollection<Daemon> ImportList()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Environment.GetFolderPath(System.Environment.SpecialFolder.MyComputer);
            openFileDialog.Filter = "JSON (*.json)|*.json|" +
                                    "All files (*.*)|*.*";

            //Wenn eine Datei gewählt worden ist
            if (openFileDialog.ShowDialog() == true)
            {
                return LoadDaemons(openFileDialog.FileName);
            }

            return new ObservableCollection<Daemon>();
        }

        private static void SaveDaemons(ObservableCollection<Daemon> daemons, string fullPath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(daemons);

                File.WriteAllText(fullPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("cant_save_deamonfile", LanguageSystem.culture) + ex.Message);
            }

        }

        private static ObservableCollection<Daemon> LoadDaemons(string fullPath)
        {
            try
            {
                string json = String.Empty;
                ObservableCollection<Daemon> daemonList = new ObservableCollection<Daemon>();

                if (File.Exists(fullPath))
                {
                    json = File.ReadAllText(fullPath);
                    daemonList = JsonConvert.DeserializeObject<ObservableCollection<Daemon>>(json);
                }

                return daemonList;
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("cant_load_deamonfile", LanguageSystem.culture) + ex.Message);
                return new ObservableCollection<Daemon>();
            }
        }

        #endregion


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            REGISTRY                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Registry

        public static void SaveInRegistry(Daemon daemon)
        {
            try
            {
                using (RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\" + daemon.ServiceName + @"\Parameters"))
                {
                    serviceKey.SetValue("DisplayName", daemon.DisplayName, RegistryValueKind.String);
                    serviceKey.SetValue("ServiceName", daemon.ServiceName, RegistryValueKind.String);

                    serviceKey.SetValue("FileDir", daemon.FileDir, RegistryValueKind.String);
                    serviceKey.SetValue("FileName", daemon.FileName, RegistryValueKind.String);

                    serviceKey.SetValue("Parameter", daemon.Parameter, RegistryValueKind.String);
                    serviceKey.SetValue("UserName", daemon.UserName, RegistryValueKind.String);
                    serviceKey.SetValue("UserPassword", daemon.UserPassword, RegistryValueKind.String);
                    serviceKey.SetValue("MaxRestarts", daemon.MaxRestarts, RegistryValueKind.DWord);

                    serviceKey.SetValue("ProcessKillTime", daemon.ProcessKillTime, RegistryValueKind.DWord);
                    serviceKey.SetValue("ProcessRestartDelay", daemon.ProcessRestartDelay, RegistryValueKind.DWord);

                    serviceKey.SetValue("ConsoleApplication", daemon.ConsoleApplication, RegistryValueKind.DWord);
                    serviceKey.SetValue("UseCtrlC", daemon.UseCtrlC, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("data_cant_be_saved", LanguageSystem.culture) + ex.Message, LanguageSystem.resManager.GetString("error", LanguageSystem.culture), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static ObservableCollection<Daemon> LoadFromRegistry()
        {
            try
            {
                ObservableCollection<Daemon> daemons = new ObservableCollection<Daemon>();

                ServiceController[] sc = ServiceController.GetServices();

                foreach (ServiceController service in sc)
                {
                    try
                    {
                        if (service.ServiceName.Contains("DaemonMaster_"))
                        {
                            daemons.Add(LoadDaemonFromRegistry(service.ServiceName));
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
                return daemons;
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageSystem.resManager.GetString("cant_load_deamonfile", LanguageSystem.culture) + ex.Message);
                return new ObservableCollection<Daemon>();
            }
        }

        private static Daemon LoadDaemonFromRegistry(string serviceName)
        {

            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName + @"\Parameters", false))
            {
                if (key == null)
                    throw new Exception("Can't open registry key!");

                Daemon daemon = new Daemon();

                daemon.DisplayName = (string)key.GetValue("DisplayName");
                daemon.ServiceName = (string)key.GetValue("ServiceName");

                daemon.FileDir = (string)key.GetValue("FileDir");
                daemon.FileName = (string)key.GetValue("FileName");

                daemon.Parameter = (string)(key.GetValue("Parameter") ?? String.Empty);
                daemon.UserName = (string)(key.GetValue("UserName") ?? String.Empty);
                daemon.UserPassword = (string)(key.GetValue("UserPassword") ?? String.Empty);
                daemon.MaxRestarts = (int)(key.GetValue("MaxRestarts") ?? 3);

                daemon.ProcessKillTime = (int)(key.GetValue("ProcessKillTime") ?? 5000);
                daemon.ProcessRestartDelay = (int)(key.GetValue("ProcessRestartDelay") ?? 0);

                daemon.ConsoleApplication = Convert.ToBoolean((key.GetValue("ConsoleApplication") ?? false));
                daemon.UseCtrlC = Convert.ToBoolean((key.GetValue("UseCtrlC") ?? false));

                return daemon;
            }
        }

        #endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             Other                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        //Gibt das Icon der Datei zurück
        public static ImageSource GetIcon(string fullPath)
        {
            try
            {
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(fullPath))
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception)
            {
                return null;
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
    }
}