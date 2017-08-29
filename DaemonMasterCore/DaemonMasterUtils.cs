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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;
using Shell32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace DaemonMasterCore
{
    public static class DaemonMasterUtils
    {
        //Gibt das Icon der Datei zurück
        public static ImageSource GetIcon(string fullPath)
        {
            try
            {
                using (System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(fullPath))
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

        public static ShortcutInfo GetShortcutInfos(string shortcutFullPath)
        {
            string directory = Path.GetDirectoryName(shortcutFullPath);
            string file = Path.GetFileName(shortcutFullPath);

            Shell shell = new Shell();
            Folder folder = shell.NameSpace(directory);
            FolderItem folderItem = folder.ParseName(file);

            ShellLinkObject link = (ShellLinkObject)folderItem.GetLink;

            ShortcutInfo shortcutInfo = new ShortcutInfo()
            {
                FilePath = link.Path,
                Arguments = link.Arguments,
                WorkingDir = link.WorkingDirectory,
                Description = link.Description,
            };

            return shortcutInfo;
        }

        public static bool IsShortcut(string shortcutFullPath)
        {
            string directory = Path.GetDirectoryName(shortcutFullPath);
            string file = Path.GetFileName(shortcutFullPath);

            Shell shell = new Shell();
            Folder folder = shell.NameSpace(directory);
            FolderItem folderItem = folder.ParseName(file);

            if (folderItem != null)
                return folderItem.IsLink;

            return false;
        }

        public static StringBuilder ConvertListToDoubleNullTerminatedString(ObservableCollection<string> stringList)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string item in stringList)
            {
                stringBuilder.Append(item.Trim()).Append("\0");
            }
            stringBuilder.Append("\0");

            return stringBuilder;
        }

        public static StringBuilder ConvertStringArrayToDoubleNullTerminatedString(string[] stringArray)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string item in stringArray)
            {
                stringBuilder.Append(item.Trim()).Append("\0");
            }
            stringBuilder.Append("\0");

            return stringBuilder;
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

    public class ShortcutInfo
    {
        public string FilePath { get; set; }
        public string WorkingDir { get; set; }
        public string Arguments { get; set; }
        public string Description { get; set; }
    }
}
