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
using System.ServiceProcess;
using System.Windows.Media;
using Microsoft.Win32;

namespace DaemonMasterCore
{
    public class ServiceListViewItem
    {
        private ImageSource _icon;
        private string _fullPath;

        public string DisplayName { get; set; }
        public string ServiceName { get; set; }

        public ServiceControllerStatus ServiceState { get; set; }

        public int? ServicePid { get; set; }
        public int? ProcessPid { get; set; }

        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                //Get the new Icon
                _icon = DaemonMasterUtils.GetIcon(value);
            }
        }

        public ImageSource Icon => _icon;

        /// <summary>
        /// Return a bool that indicate that the service run under LocalSystem rights
        /// </summary>
        /// <returns>Returns false if parameter not exist</returns>
        public bool UseLocalSystem => Convert.ToBoolean(RegistryManagement.GetParameterFromRegistry(ServiceName, "UseLocalSystem"));

        //Create the Item with the ServiceStartInfos
        public static ServiceListViewItem CreateItemFromInfo(ServiceStartInfo startInfo)
        {
            ServiceListViewItem item = new ServiceListViewItem()
            {
                DisplayName = startInfo.DisplayName,
                ServiceName = startInfo.ServiceName,
                FullPath = startInfo.FullPath,
            };

            return item;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                             METHODS                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update ServicePID and Status of the serviceItem
        /// </summary>
        public void UpdateStatus()
        {
            using (ServiceController serviceController = new ServiceController(ServiceName))
            {
                if (serviceController.Status == ServiceControllerStatus.Running)
                    serviceController.ExecuteCommand((int)ServiceCommands.UpdateInfos);

                ServiceState = serviceController.Status;
            }

            int servicePid = ServiceManagement.GetServicePID(ServiceName);
            if (servicePid <= 0)
            {
                ServicePid = null;
            }
            else
            {
                ServicePid = servicePid;
            }

            using (RegistryKey processKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + ServiceName + @"\ProcessInfo", false))
            {
                if (processKey == null)
                    return;

                int processPid = (int)processKey.GetValue("ProcessPid", -1);

                if (processPid <= 0)
                {
                    ProcessPid = null;
                }
                else
                {
                    ProcessPid = processPid;
                }


                processKey.Close();
            }
        }
    }
}
