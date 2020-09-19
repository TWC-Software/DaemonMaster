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

using DaemonMaster.Core.Config;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ServiceProcess;

namespace DaemonMaster.Core
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

        public static void SaveInRegistry(DmServiceDefinition serviceDefinition)
        {
            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegPath + serviceDefinition.ServiceName, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                //Open Parameters SubKey
                using (RegistryKey parameters = key.CreateSubKey("Parameters", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    //Strings
                    parameters.SetValue("BinaryPath", serviceDefinition.BinaryPath, RegistryValueKind.String);
                    parameters.SetValue("Arguments", serviceDefinition.Arguments, RegistryValueKind.String);

                    //Ints
                    parameters.SetValue("ProcessMaxRestarts", serviceDefinition.ProcessMaxRestarts, RegistryValueKind.DWord);
                    parameters.SetValue("ProcessTimeoutTime", serviceDefinition.ProcessTimeoutTime, RegistryValueKind.DWord);
                    parameters.SetValue("ProcessRestartDelay", serviceDefinition.ProcessRestartDelay, RegistryValueKind.DWord);
                    parameters.SetValue("CounterResetTime", serviceDefinition.CounterResetTime, RegistryValueKind.DWord);
                    parameters.SetValue("ProcessPriority", serviceDefinition.ProcessPriority, RegistryValueKind.DWord);

                    //Bools
                    parameters.SetValue("IsConsoleApplication", serviceDefinition.IsConsoleApplication, RegistryValueKind.DWord);
                    parameters.SetValue("UseCtrlC", serviceDefinition.UseCtrlC, RegistryValueKind.DWord);
                    parameters.SetValue("CanInteractWithDesktop", serviceDefinition.CanInteractWithDesktop, RegistryValueKind.DWord);
                    parameters.SetValue("UseEventLog", serviceDefinition.UseEventLog, RegistryValueKind.DWord);
                }


                //Create an give the user the permission to write to this key (needed for save the PID of the process if it's not the LocalSystem account)
                using (RegistryKey processInfo = key.CreateSubKey("ProcessInfo", RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    #region Setting permissions
                    //Only needed when user account has changed
                    if (processInfo != null && !Equals(serviceDefinition.Credentials, ServiceCredentials.NoChange))
                    {
                        //Create a new RegistrySecurity object
                        var rs = new RegistrySecurity();

                        //  Author: Nick Sarabyn - https://stackoverflow.com/questions/3282656/setting-inheritance-and-propagation-flags-with-set-acl-and-powershell
                        //  ╔═════════════╦═════════════╦═════════════════════════════════╦══════════════════════════╦══════════════════╦═════════════════════════╦═══════════════╦═════════════╗
                        //  ║             ║ folder only ║ folder, sub - folders and files ║ folder and sub - folders ║ folder and files ║ sub - folders and files ║ sub - folders ║    files    ║
                        //  ╠═════════════╬═════════════╬═════════════════════════════════╬══════════════════════════╬══════════════════╬═════════════════════════╬═══════════════╬═════════════╣
                        //  ║ Propagation ║ none        ║ none                            ║ none                     ║ none             ║ InheritOnly             ║ InheritOnly   ║ InheritOnly ║
                        //  ║ Inheritance ║ none        ║ Container|Object                ║ Container                ║ Object           ║ Container|Object        ║ Container     ║ Object      ║
                        //  ╚═════════════╩═════════════╩═════════════════════════════════╩══════════════════════════╩══════════════════╩═════════════════════════╩═══════════════╩═════════════╝

                        ////Add access rule for user (only when it is not LocalSystem)                                          
                        if (Equals(serviceDefinition.Credentials, ServiceCredentials.LocalSystem))
                        {
                            rs.AddAccessRule(new RegistryAccessRule((NTAccount)new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null).Translate(typeof(NTAccount)), RegistryRights.WriteKey, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                        }
                        else if (Equals(serviceDefinition.Credentials, ServiceCredentials.LocalService))
                        {
                            rs.AddAccessRule(new RegistryAccessRule((NTAccount)new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null).Translate(typeof(NTAccount)), RegistryRights.WriteKey, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                        }
                        else if (Equals(serviceDefinition.Credentials, ServiceCredentials.NetworkService))
                        {
                            rs.AddAccessRule(new RegistryAccessRule((NTAccount)new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null).Translate(typeof(NTAccount)), RegistryRights.WriteKey, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                        }
                        else
                        {
                            string account = DaemonMasterUtils.IsLocalDomain(serviceDefinition.Credentials.Username) ? DaemonMasterUtils.GetLoginFromUsername(serviceDefinition.Credentials.Username) : serviceDefinition.Credentials.Username;
                            rs.AddAccessRule(new RegistryAccessRule(new NTAccount(account), RegistryRights.WriteKey, InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                        }

                        processInfo.SetAccessControl(rs);

                        #endregion
                    }
                }
            }
        }

        public static DmServiceDefinition LoadFromRegistry(string serviceName)
        {
            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName, RegistryKeyPermissionCheck.ReadSubTree))
            {

                var serviceDefinition = new DmServiceDefinition(Convert.ToString(serviceName))
                {
                    DisplayName = Convert.ToString(key.GetValue("DisplayName")),
                    Description = Convert.ToString(key.GetValue("Description", string.Empty)),
                    DependOnService = (string[])key.GetValue("DependOnService", Array.Empty<string>()),
                    DependOnGroup = (string[])key.GetValue("DependOnGroup", Array.Empty<string>()),
                    LoadOrderGroup = Convert.ToString(key.GetValue("Group", string.Empty)),
                    DelayedStart = Convert.ToBoolean(key.GetValue("DelayedAutostart", false)),
                    StartType = (Advapi32.ServiceStartType)Convert.ToUInt32(key.GetValue("Start", 2))
                };


                string username = Convert.ToString(key.GetValue("ObjectName", ""));
                if (string.IsNullOrWhiteSpace(username))
                {
                    serviceDefinition.Credentials = ServiceCredentials.LocalSystem;
                }
                else
                {
                    serviceDefinition.Credentials = new ServiceCredentials(username, null);
                }

                //Open Parameters SubKey
                using (RegistryKey parameters = key.OpenSubKey("Parameters", RegistryKeyPermissionCheck.ReadSubTree))
                {
                    serviceDefinition.BinaryPath = Convert.ToString(parameters.GetValue("BinaryPath"));
                    serviceDefinition.Arguments = Convert.ToString(parameters.GetValue("Arguments", string.Empty));
                    serviceDefinition.ProcessMaxRestarts = Convert.ToInt32(parameters.GetValue("ProcessMaxRestarts", 3));
                    serviceDefinition.ProcessTimeoutTime = Convert.ToInt32(parameters.GetValue("ProcessTimeoutTime", 9500));
                    serviceDefinition.ProcessRestartDelay = Convert.ToInt32(parameters.GetValue("ProcessRestartDelay", 2000));
                    serviceDefinition.CounterResetTime = Convert.ToInt32(parameters.GetValue("CounterResetTime", 43200));
                    serviceDefinition.ProcessPriority = (ProcessPriorityClass)parameters.GetValue("ProcessPriority", ProcessPriorityClass.Normal);
                    serviceDefinition.IsConsoleApplication = Convert.ToBoolean(parameters.GetValue("IsConsoleApplication", false));
                    serviceDefinition.UseCtrlC = Convert.ToBoolean(parameters.GetValue("UseCtrlC", false));
                    serviceDefinition.CanInteractWithDesktop = Convert.ToBoolean(parameters.GetValue("CanInteractWithDesktop", false));
                    serviceDefinition.UseEventLog = Convert.ToBoolean(parameters.GetValue("UseEventLog", false));

                    return serviceDefinition;
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



        public static List<DmServiceDefinition> GetInstalledServices()
        {
            var services = new List<DmServiceDefinition>();

            using RegistryKey mainKey = Registry.LocalMachine.OpenSubKey(RegPath, RegistryKeyPermissionCheck.ReadSubTree);
            if (mainKey == null)
                return services;

            foreach (string serviceName in mainKey.GetSubKeyNames())
            {
                using (RegistryKey key = mainKey.OpenSubKey(serviceName, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    //If the key invalid, skip this service
                    if (key == null)
                        continue;

                    //Get the exe path of the service to determine later if its a service from DaemonMaster
                    string serviceExePath = Convert.ToString(key.GetValue("ImagePath"));

                    //Check service path
                    if (string.IsNullOrWhiteSpace(serviceExePath) || !DaemonMasterUtils.ComparePaths(serviceExePath, ServiceControlManager.DmServiceExe))
                        continue;

                    var serviceDefinition = new DmServiceDefinition(serviceName)
                    {
                        DisplayName = Convert.ToString(key.GetValue("DisplayName")),
                        Credentials = new ServiceCredentials(Convert.ToString(key.GetValue("ObjectName", ServiceCredentials.LocalSystem)), null),
                    };

                    using (RegistryKey parameters = key.OpenSubKey("Parameters", RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        //If the key invalid, skip it (this key is not important for the service)
                        if (parameters != null)
                        {
                            serviceDefinition.BinaryPath = Convert.ToString(parameters.GetValue("BinaryPath"));
                        }
                    }

                    services.Add(serviceDefinition);
                }
            }


            if (ConfigManagement.GetConfig.UseCompatibilityModeForSearchSystem)
            {
                services.AddRange(GetInstalledServicesCompMode().Where(x => services.All(y => y.ServiceName != x.ServiceName)));
            }

            return services;
        }

        [Obsolete("Just for compatibility mode here.", false)]
        private static List<DmServiceDefinition> GetInstalledServicesCompMode()
        {
            var daemons = new List<DmServiceDefinition>();

            ServiceController[] sc = ServiceController.GetServices();

            foreach (ServiceController service in sc)
            {
                if (!service.ServiceName.Contains("DaemonMaster_"))
                    continue;

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + service.ServiceName, false))
                {
                    if (key == null)
                        throw new Exception("Can't open registry key!");

                    var serviceDefinition = new DmServiceDefinition(Convert.ToString(service.ServiceName))
                    {
                        DisplayName = service.DisplayName, //Convert.ToString(key.GetValue("DisplayName")),
                        Credentials = new ServiceCredentials(Convert.ToString(key.GetValue("ObjectName", ServiceCredentials.LocalSystem)), null),
                    };

                    using (RegistryKey parameters = key.OpenSubKey("Parameters", false))
                    {
                        serviceDefinition.BinaryPath = Convert.ToString(parameters.GetValue("BinaryPath"));
                    }
                    daemons.Add(serviceDefinition);
                }
            }
            return daemons;
        }


        public static void WriteSessionUsername(string serviceName, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("WriteSessionUsername: Invalid username.");

            using (RegistryKey key = Registry.LocalMachine.CreateSubKey(RegPath + serviceName + "\\Parameters", true))
            {
                key.SetValue("StartInSessionAs", username, RegistryValueKind.String);
            }
        }

        public static string ReadAndClearSessionUsername(string serviceName)
        {
            //TODO: better method
            string username;

            //Just read here, so that restricted services crash not here
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName + "\\Parameters", false))
            {
                username = Convert.ToString(key.GetValue("StartInSessionAs", string.Empty));
            }
            
            if (string.IsNullOrWhiteSpace(username))
                return string.Empty;

            //Write only when a name was in it
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPath + serviceName + "\\Parameters", true))
            {
                key.SetValue("StartInSessionAs", string.Empty, RegistryValueKind.String);
            }

            return username;
        }



        public static bool IsDaemonMasterService(string serviceName)
        {
            try
            {
                //For new system
                using (RegistryKey keyNew = Registry.LocalMachine.OpenSubKey(RegPath + serviceName, false))
                {
                    if (keyNew != null)
                    {
                        //Get the exe path of the service to determine later if its a service from DaemonMaster
                        string serviceExePath = Convert.ToString(keyNew.GetValue("ImagePath") ?? string.Empty);

                        //Check the path
                        if (!string.IsNullOrWhiteSpace(serviceExePath) && DaemonMasterUtils.ComparePaths(serviceExePath, ServiceControlManager.DmServiceExe))
                            return true; //New system
                    }
                }

                return false;

            }
            catch
            {
                return false;
            }
        }

        public static string[] GetAllServiceGroups()
        {
            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RegPathServiceGroups, false))
            {
                return (string[])key.GetValue("List", string.Empty);
            }
        }



        public static bool EnableInteractiveServices(bool enable)
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", true))
                {
                    if (regKey == null)
                        return false;

                    regKey.SetValue("NoInteractiveServices", enable ? 0 : 1, RegistryValueKind.DWord);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool CheckInteractiveServices()
        {
            try
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Windows", false))
                {
                    return regKey != null && !Convert.ToBoolean(regKey.GetValue("NoInteractiveServices", true));
                }
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
