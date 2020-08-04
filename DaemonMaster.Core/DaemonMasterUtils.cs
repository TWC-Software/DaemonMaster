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

using System;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;

namespace DaemonMaster.Core
{
    public static class DaemonMasterUtils
    {
        public static bool IsSupportedWindows10VersionForIwd
        {
            get
            {
                return Config.ConfigManagement.GetConfig.UnlockInteractiveServiceCreationOnNotSupportedSystem || Environment.OSVersion.Version.Major < 10 || (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build < 17134);
            }
        }

        public static bool IsNt
        {
            get { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
        }


        public static bool CheckUi0DetectService()
        {
            try
            {
                using (ServiceController scManager = new ServiceController("UI0Detect"))
                {
                    if (scManager.Status == ServiceControllerStatus.Running)
                        return true;

                    if (scManager.Status != ServiceControllerStatus.StartPending)
                        scManager.Start();

                    scManager.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(10000));

                    return scManager.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetLoginFromUsername(string s)
        {
            int stop = s.LastIndexOf("\\", StringComparison.Ordinal); //last index because textbox output is like this ".\\\\Olaf"
            return (stop > -1) ? s.Substring(stop + 1, s.Length - stop - 1) : string.Empty;
        }

        public static string GetDomainFromUsername(string s)
        {
            int stop = s.IndexOf("\\", StringComparison.Ordinal);
            string domainName = (stop > -1) ? s.Substring(0, stop) : string.Empty; //when nothing is there make the string empty
            return (domainName != ".") ? domainName : string.Empty; // "." stands also for local domain so make it empty
        }

        public static bool IsLocalDomain(string s)
        {
            string domainName = GetDomainFromUsername(s);
            return domainName == string.Empty || domainName == ".";
        }

        public static bool ValidateUser(string username, SecureString password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException(nameof(username));

            if (password == null)
                throw new ArgumentNullException(nameof(password));


            try
            {
                using (TokenHandle tokenHandle = TokenHandle.GetTokenFromLogon(username, password, Advapi32.LogonType.Interactive))
                {
                    return !tokenHandle.IsInvalid;
                }
            }
            catch (Win32Exception)
            {
                return false;
            }
        }

        public static bool IsElevated()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        //Source: https://social.msdn.microsoft.com/Forums/vstudio/en-US/a979351c-800f-41e7-b153-2d53ff6aac29/how-to-get-running-windows-service-process-id-?forum=netfxbcl
        public static uint GetProcessIdByServiceName(string serviceName)
        {
            uint processId = 0;
            string qry = "SELECT PROCESSID FROM WIN32_SERVICE WHERE NAME = '" + serviceName + "'";
            System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(qry);
            foreach (System.Management.ManagementObject mngntObj in searcher.Get())
            {
                processId = (uint)mngntObj["PROCESSID"];
            }

            return processId;
        }

        public static string ConvertNullTerminatedStringToString(this string s)
        {
            return s.TrimEnd('\0');
        }

        public static string SurroundWithDoubleQuotes(this string s)
        {
            return SurroundWith(s, "\"");
        }

        public static string SurroundWith(this string s, string value)
        {
            return value + s + value;
        }

        public static bool ComparePaths(string path1, string path2)
        {
            return string.Equals(path1.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                path2.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
