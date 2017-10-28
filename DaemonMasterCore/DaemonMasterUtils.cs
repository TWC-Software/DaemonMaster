/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: DaemonMasterUtils
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

using DaemonMasterCore.Win32;
using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Management;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Media;

namespace DaemonMasterCore
{
    public static class DaemonMasterUtils
    {
        /// <summary>
        /// GIve the icon of an .exe or file
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public static ImageSource GetIcon(string fullPath)
        {
            try
            {
                //Get the real filePath if it's a shortcut
                if (ShellLinkWrapper.IsShortcut(fullPath))
                {
                    using (ShellLinkWrapper shellLinkWrapper = new ShellLinkWrapper(fullPath))
                    {
                        fullPath = shellLinkWrapper.FilePath;
                    }
                }

                using (Icon icon = Icon.ExtractAssociatedIcon(fullPath))
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        //From: http://stackoverflow.com/questions/1841790/how-can-a-windows-service-determine-its-servicename, 02.05.2017
        public static String GetServiceName()
        {
            // Calling System.ServiceProcess.ServiceBase::ServiceNamea allways returns
            // an empty string,
            // see https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=387024

            // So we have to do some more work to find out our service name, this only works if
            // the process contains a single service, if there are more than one services hosted
            // in the process you will have to do something else

            int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            String query = "SELECT * FROM Win32_Service where ProcessId = " + processId;
            ManagementObjectSearcher searcher =
                new ManagementObjectSearcher(query);

            foreach (ManagementObject queryObj in searcher.Get())
            {
                return queryObj["Name"].ToString();
            }

            throw new Exception("Can not get the ServiceName");
        }

        public static string GetDisplayName(string serviceName)
        {
            using (ServiceController sc = new ServiceController(serviceName))
            {
                return sc.DisplayName;
            }
        }

        public static void ExportItem(string serviceName, string path)
        {
            Daemon daemon = RegistryManagement.LoadDaemonFromRegistry(serviceName);

            using (StreamWriter streamWriter = File.CreateText(path))
            {
                JsonSerializer serializer = new JsonSerializer()
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(streamWriter, daemon);
            }
        }

        public static Daemon ImportItem(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            using (StreamReader streamReader = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                Daemon daemon = (Daemon)serializer.Deserialize(streamReader, typeof(Daemon));
                return daemon;
            }
        }
    }
}
