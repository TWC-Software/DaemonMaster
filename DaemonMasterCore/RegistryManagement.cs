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
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
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
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegPath + serviceStartInfo.ServiceName))
            {

                //Open Parameters SubKey
                using (RegistryKey parameters = key.CreateSubKey("Parameters"))
                {
                    //Strings
                    parameters.SetValue("FileDir", serviceStartInfo.FileDir, RegistryValueKind.String);
                    parameters.SetValue("FileName", serviceStartInfo.FileName, RegistryValueKind.String);
                    parameters.SetValue("FileExtension", serviceStartInfo.FileExtension, RegistryValueKind.String);
                    parameters.SetValue("Parameter", serviceStartInfo.Parameter, RegistryValueKind.String);

                    //Ints
                    parameters.SetValue("MaxRestarts", serviceStartInfo.MaxRestarts, RegistryValueKind.DWord);
                    parameters.SetValue("ProcessKillTime", serviceStartInfo.ProcessKillTime, RegistryValueKind.DWord);
                    parameters.SetValue("ProcessRestartDelay", serviceStartInfo.ProcessRestartDelay, RegistryValueKind.DWord);
                    parameters.SetValue("CounterResetTime", serviceStartInfo.CounterResetTime, RegistryValueKind.DWord);

                    //Bools
                    parameters.SetValue("UseLocalSystem", serviceStartInfo.UseLocalSystem, RegistryValueKind.DWord);
                    parameters.SetValue("ConsoleApplication", serviceStartInfo.ConsoleApplication, RegistryValueKind.DWord);
                    parameters.SetValue("UseCtrlC", serviceStartInfo.UseCtrlC, RegistryValueKind.DWord);
                    parameters.SetValue("CanInteractWithDesktop", serviceStartInfo.CanInteractWithDesktop, RegistryValueKind.DWord);

                    parameters.Close();
                }


                //Create an give the user the permission to write to this key (needed for save the PID of the process if it's not the LocalSystem account)
                using (RegistryKey processInfo = key.CreateSubKey("ProcessInfo"))
                {
                    #region Setting permissions

                    //Create a new RegistrySecurity object
                    RegistrySecurity rs = new RegistrySecurity();

                    //  Author: Nick Sarabyn - https://stackoverflow.com/questions/3282656/setting-inheritance-and-propagation-flags-with-set-acl-and-powershell
                    //  ╔═════════════╦═════════════╦═════════════════════════════════╦══════════════════════════╦══════════════════╦═════════════════════════╦═══════════════╦═════════════╗
                    //  ║             ║ folder only ║ folder, sub - folders and files ║ folder and sub - folders ║ folder and files ║ sub - folders and files ║ sub - folders ║    files    ║
                    //  ╠═════════════╬═════════════╬═════════════════════════════════╬══════════════════════════╬══════════════════╬═════════════════════════╬═══════════════╬═════════════╣
                    //  ║ Propagation ║ none        ║ none                            ║ none                     ║ none             ║ InheritOnly             ║ InheritOnly   ║ InheritOnly ║
                    //  ║ Inheritance ║ none        ║ Container|Object                ║ Container                ║ Object           ║ Container|Object        ║ Container     ║ Object      ║
                    //  ╚═════════════╩═════════════╩═════════════════════════════════╩══════════════════════════╩══════════════════╩═════════════════════════╩═══════════════╩═════════════╝

                    ////Add access rule for user (only when it is not LocalSystem)
                    bool isLocalSystem = serviceStartInfo.UseLocalSystem || String.IsNullOrWhiteSpace(serviceStartInfo.Username) || serviceStartInfo.Username == "LocalSystem";
                    if (isLocalSystem)
                    {
                        rs.AddAccessRule(new RegistryAccessRule((NTAccount)new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), RegistryRights.WriteKey, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    }
                    else
                    {
                        rs.AddAccessRule(new RegistryAccessRule(new NTAccount(DaemonMasterUtils.GetDomainFromUsername(serviceStartInfo.Username), DaemonMasterUtils.GetLoginFromUsername(serviceStartInfo.Username)), RegistryRights.WriteKey, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    }

                    processInfo.SetAccessControl(rs);
                    #endregion

                    processInfo.Close();
                }
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
                    serviceStartInfo.CanInteractWithDesktop = Convert.ToBoolean(parameters.GetValue("CanInteractWithDesktop", false));

                    return serviceStartInfo;
                }
            }
        }

        public static object GetParameterFromRegistry(string serviceName, string parameterName, string subkey = "\\Parameters")
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName + subkey, false))
            {
                return key.GetValue(parameterName, null);
            }
        }

        public static ObservableCollection<ServiceListViewItem> LoadDaemonItemsFromRegistry()
        {
            ObservableCollection<ServiceListViewItem> daemons = new ObservableCollection<ServiceListViewItem>();

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath, false))
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    using (RegistryKey subKey = key.OpenSubKey(subKeyName, false))
                    {
                        //if the sub key is invalid skip this service
                        if (subKey == null)
                            continue;

                        //Get the exe path of the service to determine later if its a service from DaemonMaster
                        string serviceExePath = Convert.ToString(subKey.GetValue("ImagePath") ?? String.Empty);

                        //If the serviceExePath is invalid skip this service
                        if (String.IsNullOrWhiteSpace(serviceExePath))
                            continue;

                        if (serviceExePath.Contains(ServiceManagement.GetServiceExePath()))
                        {
                            ServiceListViewItem serviceListViewItem = new ServiceListViewItem
                            {
                                ServiceName = Path.GetFileName(subKey.Name),
                                DisplayName = Convert.ToString(subKey.GetValue("DisplayName"))
                            };

                            using (RegistryKey parmSubKey = subKey.OpenSubKey("Parameters", false))
                            {
                                //If the parameters sub key invalid, skip this service
                                if (parmSubKey == null)
                                    continue;

                                serviceListViewItem.FullPath = Convert.ToString(parmSubKey.GetValue("FileDir")) + @"/" + Convert.ToString(parmSubKey.GetValue("FileName"));
                            }

                            daemons.Add(serviceListViewItem);
                        }
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
