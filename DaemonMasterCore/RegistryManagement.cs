/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: RegistryManagement
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
using System.Collections.ObjectModel;
using System.ServiceProcess;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32;

namespace DaemonMasterCore
{
    public static class RegistryManagement
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            REGISTRY                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Registry

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              CONST                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////


        private const string RegPath = @"SYSTEM\CurrentControlSet\Services\";
        private const string RegPathServiceGroups = @"SYSTEM\CurrentControlSet\Control\ServiceGroupOrder\";

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             METHODS                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void SaveInRegistry(ServiceStartInfo serviceStartInfo)
        {
            using (RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(RegPath + serviceStartInfo.ServiceName + @"\Parameters"))
            {
                //Strings
                serviceKey.SetValue("FileDir", serviceStartInfo.FileDir, RegistryValueKind.String);
                serviceKey.SetValue("FileName", serviceStartInfo.FileName, RegistryValueKind.String);
                serviceKey.SetValue("FileExtension", serviceStartInfo.FileExtension, RegistryValueKind.String);
                serviceKey.SetValue("Parameter", serviceStartInfo.Parameter, RegistryValueKind.String);

                //Ints
                serviceKey.SetValue("MaxRestarts", serviceStartInfo.MaxRestarts, RegistryValueKind.DWord);
                serviceKey.SetValue("ProcessKillTime", serviceStartInfo.ProcessKillTime, RegistryValueKind.DWord);
                serviceKey.SetValue("ProcessRestartDelay", serviceStartInfo.ProcessRestartDelay, RegistryValueKind.DWord);
                serviceKey.SetValue("CounterResetTime", serviceStartInfo.CounterResetTime, RegistryValueKind.DWord);

                //Bools
                serviceKey.SetValue("UseLocalSystem", serviceStartInfo.UseLocalSystem, RegistryValueKind.DWord);
                serviceKey.SetValue("ConsoleApplication", serviceStartInfo.ConsoleApplication, RegistryValueKind.DWord);
                serviceKey.SetValue("UseCtrlC", serviceStartInfo.UseCtrlC, RegistryValueKind.DWord);

                serviceKey.Close();
            }
        }

        public static ServiceStartInfo LoadServiceStartInfosFromRegistry(string serviceName)
        {
            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName, false))
            {
                if (key == null)
                    throw new Exception("Can't open registry key! (General)");

                ServiceStartInfo serviceStartInfo = new ServiceStartInfo
                {
                    ServiceName = Convert.ToString(serviceName),
                    DisplayName = Convert.ToString(key.GetValue("DisplayName")),
                    Username = Convert.ToString(key.GetValue("ObjectName", null)),
                    Description = Convert.ToString(key.GetValue("Description", String.Empty)),
                    DependOnService = (string[])key.GetValue("DependOnService", new string[0]),
                    DependOnGroup = (string[])key.GetValue("DependOnGroup", new string[0]),
                    DelayedStart = Convert.ToBoolean(key.GetValue("DelayedAutostart", false)),
                    StartType = (NativeMethods.SERVICE_START)Convert.ToUInt32(key.GetValue("Start", 2))
                };


                //Open Parameters SubKey
                using (RegistryKey parameters = key.OpenSubKey("Parameters", false))
                {
                    if (parameters == null)
                        throw new Exception("Can't open registry key! (Parameters)");

                    serviceStartInfo.FileDir = Convert.ToString(parameters.GetValue("FileDir"));
                    serviceStartInfo.FileName = Convert.ToString(parameters.GetValue("FileName"));
                    serviceStartInfo.FileExtension = Convert.ToString(parameters.GetValue("FileExtension"));
                    serviceStartInfo.Parameter = Convert.ToString(parameters.GetValue("Parameter", String.Empty));
                    serviceStartInfo.MaxRestarts = Convert.ToInt32(parameters.GetValue("MaxRestarts", 3));
                    serviceStartInfo.ProcessKillTime = Convert.ToInt32(parameters.GetValue("ProcessKillTime", 9500));
                    serviceStartInfo.ProcessRestartDelay = Convert.ToInt32(parameters.GetValue("ProcessRestartDelay", 2000));
                    serviceStartInfo.CounterResetTime = Convert.ToInt32(parameters.GetValue("CounterResetTime", 43200));
                    serviceStartInfo.UseLocalSystem = Convert.ToBoolean(parameters.GetValue("UseLocalSystem"));
                    serviceStartInfo.ConsoleApplication = Convert.ToBoolean(parameters.GetValue("ConsoleApplication", false));
                    serviceStartInfo.UseCtrlC = Convert.ToBoolean(parameters.GetValue("UseCtrlC", false));

                    return serviceStartInfo;
                }
            }
        }

        public static object GetParameterFromRegistry(string serviceName, string parameterName)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName + @"\Parameters", false))
            {
                return key.GetValue(parameterName, null);
            }
        }

        public static ObservableCollection<ServiceListViewItem> LoadDaemonItemsFromRegistry()
        {
            ObservableCollection<ServiceListViewItem> daemons = new ObservableCollection<ServiceListViewItem>();

            ServiceController[] sc = ServiceController.GetServices();

            foreach (ServiceController service in sc)
            {
                if (service.ServiceName.Contains("DaemonMaster_"))
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + service.ServiceName + @"\Parameters", false))
                    {
                        if (key == null)
                            throw new Exception("Can't open registry key!");

                        ServiceListViewItem serviceListViewItem = new ServiceListViewItem
                        {
                            DisplayName = service.DisplayName,
                            ServiceName = service.ServiceName,
                            FullPath = (string)key.GetValue("FileDir") + @"/" + (string)key.GetValue("FileName")
                        };

                        daemons.Add(serviceListViewItem);
                    }
                }
            }
            return daemons;
        }

        public static string[] GetAllServiceGroups()
        {
            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPathServiceGroups, false))
            {
                if (key == null)
                    throw new Exception("Can't open registry key!");

                return (string[])key.GetValue("List", String.Empty);
            }
        }





        //Set NoInteractiveServices to 0
        public static bool EnableInteractiveServices(bool enable)
        {
            //If Windows10 1803 or higher return false (UI0Detect service does not exist anymore)
            if (!DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
                return false;

            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", true))
                {
                    if (regKey == null)
                        return false;

                    regKey.SetValue("NoInteractiveServices", enable ? 0 : 1, RegistryValueKind.DWord);
                    regKey.Close();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Check if NoInteractiveServices is 0
        public static bool CheckNoInteractiveServicesRegKey()
        {
            //If Windows10 1803 or higher return false (UI0Detect service does not exist anymore)
            if (!DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
                return false;

            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", false))
                {
                    return regKey != null && !Convert.ToBoolean(regKey.GetValue("NoInteractiveServices"));
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
