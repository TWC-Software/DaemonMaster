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

        public static void SaveInRegistry(Daemon daemon)
        {
            using (RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(RegPath + daemon.ServiceName + @"\Parameters"))
            {
                //Strings
                serviceKey.SetValue("FileDir", daemon.FileDir, RegistryValueKind.String);
                serviceKey.SetValue("FileName", daemon.FileName, RegistryValueKind.String);
                serviceKey.SetValue("FileExtension", daemon.FileExtension, RegistryValueKind.String);
                serviceKey.SetValue("Parameter", daemon.Parameter, RegistryValueKind.String);


                if (!daemon.UseLocalSystem)
                {
                    serviceKey.SetValue("Password", SecurityManagement.EncryptPassword(daemon.Password), RegistryValueKind.Binary);
                    serviceKey.SetValue("Username", daemon.Username, RegistryValueKind.String);
                }
                else
                {
                    serviceKey.DeleteValue("Password", false);
                    serviceKey.DeleteValue("Username", false);
                }

                //Ints
                serviceKey.SetValue("MaxRestarts", daemon.MaxRestarts, RegistryValueKind.DWord);
                serviceKey.SetValue("ProcessKillTime", daemon.ProcessKillTime, RegistryValueKind.DWord);
                serviceKey.SetValue("ProcessRestartDelay", daemon.ProcessRestartDelay, RegistryValueKind.DWord);
                serviceKey.SetValue("CounterResetTime", daemon.CounterResetTime, RegistryValueKind.DWord);

                //Bools
                serviceKey.SetValue("UseLocalSystem", daemon.UseLocalSystem, RegistryValueKind.DWord);
                serviceKey.SetValue("ConsoleApplication", daemon.ConsoleApplication, RegistryValueKind.DWord);
                serviceKey.SetValue("UseCtrlC", daemon.UseCtrlC, RegistryValueKind.DWord);

                serviceKey.Close();
            }
        }

        public static Daemon LoadDaemonFromRegistry(string serviceName)
        {
            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName, false))
            {
                if (key == null)
                    throw new Exception("Can't open registry key! (General)");

                Daemon daemon = new Daemon
                {
                    ServiceName = Convert.ToString(serviceName),
                    DisplayName = Convert.ToString(key.GetValue("DisplayName")),
                    Description = Convert.ToString(key.GetValue("Description")),
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

                    daemon.FileDir = Convert.ToString(parameters.GetValue("FileDir"));
                    daemon.FileName = Convert.ToString(parameters.GetValue("FileName"));
                    daemon.FileExtension = Convert.ToString(parameters.GetValue("FileExtension"));
                    daemon.Parameter = Convert.ToString(parameters.GetValue("Parameter"));
                    daemon.Username = Convert.ToString(parameters.GetValue("Username", String.Empty));
                    daemon.Password = SecurityManagement.DecryptPassword((byte[])parameters.GetValue("Password", new byte[0]));
                    daemon.MaxRestarts = Convert.ToInt32(parameters.GetValue("MaxRestarts", 3));
                    daemon.ProcessKillTime = Convert.ToInt32(parameters.GetValue("ProcessKillTime", 9500));
                    daemon.ProcessRestartDelay = Convert.ToInt32(parameters.GetValue("ProcessRestartDelay", 2000));
                    daemon.CounterResetTime = Convert.ToInt32(parameters.GetValue("CounterResetTime", 2000));
                    daemon.UseLocalSystem = Convert.ToBoolean(parameters.GetValue("UseLocalSystem"));
                    daemon.ConsoleApplication = Convert.ToBoolean(parameters.GetValue("ConsoleApplication", false));
                    daemon.UseCtrlC = Convert.ToBoolean(parameters.GetValue("UseCtrlC", false));

                    return daemon;
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

        public static ObservableCollection<DaemonItem> LoadDaemonItemsFromRegistry()
        {
            ObservableCollection<DaemonItem> daemons = new ObservableCollection<DaemonItem>();

            ServiceController[] sc = ServiceController.GetServices();

            foreach (ServiceController service in sc)
            {
                if (service.ServiceName.Contains("DaemonMaster_"))
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + service.ServiceName + @"\Parameters", false))
                    {
                        if (key == null)
                            throw new Exception("Can't open registry key!");

                        DaemonItem daemonItem = new DaemonItem
                        {
                            DisplayName = service.DisplayName,
                            ServiceName = service.ServiceName,
                            FullPath = (string)key.GetValue("FileDir") + @"/" + (string)key.GetValue("FileName"),
                        };

                        daemons.Add(daemonItem);
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
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", true))
                {
                    if (regKey == null)
                        return false;

                    regKey.SetValue("NoInteractiveServices", enable ? "1" : "0", RegistryValueKind.DWord);
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
