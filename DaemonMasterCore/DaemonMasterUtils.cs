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
using System.Security;
using System.Security.Principal;
using System.ServiceProcess;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke.Advapi32;

namespace DaemonMasterCore
{
    public static class DaemonMasterUtils
    {
        public static bool IsSupportedWindows10VersionOrLower()
        {
            return Environment.OSVersion.Version.Major < 10 || (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build < 17134);
        }

        public static bool CheckUI0DetectService()
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
            int stop = s.IndexOf("\\", StringComparison.Ordinal);
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
    }
}
