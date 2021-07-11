using System;
using System.Collections.Generic;
using System.Drawing;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using GalaSoft.MvvmLight;
using Microsoft.Win32;

namespace DaemonMaster.Models
{
    public class ServiceListViewItem : ObservableObject
    {
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => Set(ref _displayName, value);
        }

        private string _serviceName;
        public string ServiceName
        {
            get => _serviceName;
            set => Set(ref _serviceName, value);
        }

        private ServiceControllerStatus _serviceState;
        public ServiceControllerStatus ServiceState
        {
            get => _serviceState;
            set => Set(ref _serviceState, value);
        }

        private string _binaryPath;
        public string BinaryPath
        {

            get => _binaryPath;
            set
            {
                //Get the new BinaryIcon
                BinaryIcon = GetIcon(value);
                Set(ref _binaryPath, value);
            }
        }

        private ServiceCredentials _serviceCredentials;
        public ServiceCredentials ServiceCredentials
        {
            get => _serviceCredentials;
            set
            {
                Set(ref _serviceCredentials, value);
                UseLocalSystem = Equals(ServiceCredentials, ServiceCredentials.LocalSystem);
            }
        }

        private bool _useLocalSystem;
        public bool UseLocalSystem
        {
            get => _useLocalSystem;
            private set => Set(ref _useLocalSystem, value);
        }

        private ImageSource _binaryIcon;
        public ImageSource BinaryIcon
        {
            get => _binaryIcon;
            private set => Set(ref _binaryIcon, value);
        }

        private uint? _servicePid;
        public uint? ServicePid
        {
            get => _servicePid;
            set => Set(ref _servicePid, value);
        }

        private uint? _processPid;
        public uint? ProcessPid
        {
            get => _processPid;
            set => Set(ref _processPid, value);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                        STATIC METHODS                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static IReadOnlyList<ServiceListViewItem> GetInstalledServices()
        {
            var services = new List<ServiceListViewItem>();

            using RegistryKey mainKey = Registry.LocalMachine.OpenSubKey(RegistryManagement.ServiceRegPath, RegistryKeyPermissionCheck.ReadSubTree);
            if (mainKey == null)
                return services;

            foreach (var service in RegistryManagement.GetInstalledServices())
            {
                using (RegistryKey key = mainKey.OpenSubKey(service.ServiceName, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    //If the key invalid, skip this service
                    if (key == null)
                        continue;

                    var serviceDefinition = new ServiceListViewItem(service)
                    {
                        BinaryPath = Convert.ToString(key.GetValue("ImagePath")),
                        ProcessPid = null, //unknown
                        ServicePid = null, //unknown
                        ServiceCredentials = new ServiceCredentials(Convert.ToString(key.GetValue("ObjectName", ServiceCredentials.LocalSystem)), null),
                    };

                    using (RegistryKey parameters = key.OpenSubKey("Parameters", RegistryKeyPermissionCheck.ReadSubTree))
                    {
                        serviceDefinition.BinaryPath = Convert.ToString(parameters?.GetValue("BinaryPath", string.Empty) ?? string.Empty);
                    }

                    services.Add(serviceDefinition);
                }
            }

            return services;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                          CONSTRUCTOR                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        public ServiceListViewItem(string serviceName)
        {
            ServiceName = serviceName;
        }

        public ServiceListViewItem(ServiceInfo serviceInfo) : this(serviceInfo.ServiceName)
        {
            DisplayName = serviceInfo.DisplayName;
        }

        public ServiceListViewItem(IWin32ServiceDefinition serviceDefinition) : this(serviceDefinition.ServiceName)
        {
            DisplayName = serviceDefinition.DisplayName;
            ServiceCredentials = serviceDefinition.Credentials;
        }

        public ServiceListViewItem(DmServiceDefinition serviceDefinition) : this((IWin32ServiceDefinition)serviceDefinition)
        {
            BinaryPath = serviceDefinition.BinaryPath;
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            OVERRIDES                                                 //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gives the service name of this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance service name.
        /// </returns>
        public override string ToString()
        {
            return ServiceName;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            // Check for null and type  
            if (obj == null || !(obj is ServiceListViewItem objTyped))
                return false;

            // Check for same reference  
            if (ReferenceEquals(this, obj))
                return true;

            return ServiceName == objTyped.ServiceName;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return ServiceName.GetHashCode();
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             METHODS                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update ServicePID and Status of the serviceItem
        /// </summary>
        public async Task UpdateStatusAsync()
        {
            ServiceControllerStatus serviceState = ServiceControllerStatus.Stopped;
            uint? servicePid = null;
            uint? processPid = null;

            await Task.Run(() =>
            {
                //Set service PID
                using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
                {
                    using (ServiceHandle serviceHandle = scm.OpenService(ServiceName, Advapi32.ServiceAccessRights.QueryStatus))
                    {
                        var queryServiceStatus = serviceHandle.QueryServiceStatus();
                        if (queryServiceStatus.processId > 0)
                            servicePid = queryServiceStatus.processId;

                        serviceState = queryServiceStatus.currentState.ConvertToServiceControllerStatus();
                    }
                }

                if (servicePid > 0)
                {
                    using (RegistryKey processKey = Registry.LocalMachine.OpenSubKey(RegistryManagement.ServiceRegPath + ServiceName + @"\ProcessInfo", false))
                    {
                        if (processKey != null)
                        {
                            var processPidKeyValue = (int)processKey.GetValue("ProcessPid", -1);
                            if (processPidKeyValue > 0)
                                processPid = (uint?)processPidKeyValue;
                        }
                    }
                }
            });

            ServicePid = servicePid;
            ServiceState = serviceState;
            ProcessPid = processPid;
        }

        public void UpdateStatus()
        {
            //Set service PID
            Advapi32.ServiceStatusProcess serviceStatus;
            using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
            {
                using (ServiceHandle serviceHandle = scm.OpenService(ServiceName, Advapi32.ServiceAccessRights.QueryStatus))
                {
                    serviceStatus = serviceHandle.QueryServiceStatus();
                }
            }

            ServicePid = serviceStatus.processId <= 0 ? (uint?)null : serviceStatus.processId;
            ServiceState = Enum.TryParse(serviceStatus.currentState.ToString(), out ServiceControllerStatus outValue) ? outValue : ServiceControllerStatus.Stopped;

            if (ServicePid != null) //normally no process can run when the service has been stopped => also the service can not update this key so it is useless to query it...
            {
                //TODO: move to an other class + create class for the data like ServiceProcessInfo...
                using (RegistryKey processKey = Registry.LocalMachine.OpenSubKey(RegistryManagement.ServiceRegPath + ServiceName + @"\ProcessInfo", false))
                {
                    if (processKey == null)
                        ProcessPid = null;

                    var processPid = (int)processKey.GetValue("ProcessPid", -1);
                    ProcessPid = processPid < 0 ? null : (uint?)processPid;
                }
            }
            else
            {
                ProcessPid = null;
            }
        }


        /// <summary>
        /// Give the icon of an .exe or file
        /// </summary>
        /// <param name="binaryPath"></param>
        /// <returns></returns>
        private static ImageSource GetIcon(string binaryPath)
        {
            try
            {
                //Get the real filePath if it's a shortcut
                if (ShellLinkWrapper.IsShortcut(binaryPath))
                {
                    using (var shellLinkWrapper = new ShellLinkWrapper(binaryPath))
                    {
                        binaryPath = shellLinkWrapper.FilePath;
                    }
                }

                using (Icon icon = Icon.ExtractAssociatedIcon(binaryPath))
                {
                    if (icon == null)
                        return null;

                    return Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
