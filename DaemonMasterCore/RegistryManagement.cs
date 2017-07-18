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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ServiceProcess;

namespace DaemonMasterCore
{
    public static class RegistryManagement
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            REGISTRY                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Registry

        public static void SaveInRegistry(Daemon daemon)
        {
            using (RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\" + daemon.ServiceName + @"\Parameters"))
            {
                //Strings
                serviceKey.SetValue("FileDir", daemon.FileDir, RegistryValueKind.String);
                serviceKey.SetValue("FileName", daemon.FileName, RegistryValueKind.String);

                serviceKey.SetValue("Parameter", daemon.Parameter, RegistryValueKind.String);
                serviceKey.SetValue("Username", daemon.Username, RegistryValueKind.String);

                //Bytes
                byte[] entropy = SecurityManagement.CreateRandomEntropy();
                serviceKey.SetValue("Key", entropy, RegistryValueKind.Binary);
                serviceKey.SetValue("Password", SecurityManagement.EncryptPassword(daemon.Password, entropy), RegistryValueKind.Binary);

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
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName, false))
            {
                if (key == null)
                    throw new Exception("Can't open registry key!");

                Daemon daemon = new Daemon
                {
                    ServiceName = Convert.ToString(serviceName),
                    DisplayName = Convert.ToString(key.GetValue("DisplayName")),
                    Description = Convert.ToString(key.GetValue("Description")),
                    DependOnService = (string[])key.GetValue("DependOnService", String.Empty),
                    DelayedStart = Convert.ToBoolean(key.GetValue("DelayedAutostart", false)),
                    StartType = (NativeMethods.SERVICE_START)Convert.ToUInt32(key.GetValue("Start", 2))
                };


                //Open Parameters SubKey
                using (RegistryKey parameters = key.OpenSubKey(@"Parameters", false))
                {
                    if (parameters == null)
                        throw new Exception("Can't open registry key!");

                    daemon.FileDir = Convert.ToString(parameters.GetValue("FileDir"));
                    daemon.FileName = Convert.ToString(parameters.GetValue("FileName"));
                    daemon.Parameter = Convert.ToString(parameters.GetValue("Parameter"));
                    daemon.Username = Convert.ToString(parameters.GetValue("Username"));
                    byte[] entropy = (byte[])parameters.GetValue("Key");
                    daemon.Password = SecurityManagement.DecryptPassword((byte[])parameters.GetValue("Password"), entropy);
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

        public static ObservableCollection<DaemonInfo> LoadDaemonInfosFromRegistry()
        {
            ObservableCollection<DaemonInfo> daemons = new ObservableCollection<DaemonInfo>();

            ServiceController[] sc = ServiceController.GetServices();

            foreach (ServiceController service in sc)
            {
                try
                {
                    if (service.ServiceName.Contains("DaemonMaster_"))
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + service.ServiceName + @"\Parameters", false))
                        {
                            if (key == null)
                                throw new Exception("Can't open registry key!");

                            DaemonInfo daemonInfo = new DaemonInfo
                            {
                                DisplayName = service.DisplayName,
                                ServiceName = service.ServiceName,
                                FullPath = (string)key.GetValue("FileDir") + @"/" + (string)key.GetValue("FileName")
                            };

                            daemons.Add(daemonInfo);
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            return daemons;
        }






        //�ndert den Regkey so das Interactive Services erlaubt werden (Set NoInteractiveServices to 0)
        public static bool ActivateInteractiveServices()
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", true))
                {
                    if (regKey == null)
                        return false;

                    regKey.SetValue("NoInteractiveServices", "0", RegistryValueKind.DWord);
                    regKey.Close();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        //�ndert den Regkey so das Interactive Services erlaubt werden (Set NoInteractiveServices to 0)
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
