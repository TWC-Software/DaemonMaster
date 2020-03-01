/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: serviceItem
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
using System.Drawing;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DaemonMaster.Core;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using Microsoft.Win32;

namespace DaemonMaster
{
    public class ServiceListViewItem
    {
        private ImageSource _binaryIcon;
        private string _binaryPath;

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public ServiceControllerStatus ServiceState { get; set; }

        public uint? ServicePid { get; set; }
        public uint? ProcessPid { get; set; }

        public string BinaryPath
        {

            get => _binaryPath;
            set
            {
                _binaryPath = value;
                //Get the new BinaryIcon
                _binaryIcon = GetIcon(value);
            }
        }

        public bool UseLocalSystem { get; }

        public ImageSource BinaryIcon => _binaryIcon;

        public ServiceListViewItem(string serviceName, string displayName, string binaryPath, bool useLocalSystem)
        {
            ServiceName = serviceName;
            DisplayName = displayName;
            BinaryPath = binaryPath;
            UseLocalSystem = useLocalSystem;
        }

        public static ServiceListViewItem CreateFromServiceDefinition(DmServiceDefinition definition)
        {
            return new ServiceListViewItem(definition.ServiceName, definition.DisplayName, definition.BinaryPath, Equals(definition.Credentials, ServiceCredentials.LocalSystem));
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             METHODS                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update ServicePID and Status of the serviceItem
        /// </summary>
        public void UpdateStatus()
        {
            //Set service PID
            using (ServiceControlManager scm = ServiceControlManager.Connect(Advapi32.ServiceControlManagerAccessRights.Connect))
            {
                using (ServiceHandle serviceHandle = scm.OpenService(ServiceName, Advapi32.ServiceAccessRights.QueryStatus))
                {
                    Advapi32.ServiceStatusProcess serviceStatus = serviceHandle.QueryServiceStatus();

                    ServicePid = serviceStatus.processId <= 0 ? (uint?) null : serviceStatus.processId;
                    ServiceState = Enum.TryParse(serviceStatus.currentState.ToString(), out ServiceControllerStatus outValue) ? outValue : ServiceControllerStatus.Stopped;
                }
            }

            //Set process PID
            if (ServicePid != null) //normally no process can run when the service has been stopped
            {
                using (RegistryKey processKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + ServiceName + @"\ProcessInfo", false))
                {
                    if (processKey == null)
                        return;

                    uint processPid = Convert.ToUInt32(processKey.GetValue("ProcessPid", 0));
                    ProcessPid = processPid <= 0 ? (uint?) null : processPid;
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
