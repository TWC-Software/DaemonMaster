/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SystemManagement
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
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
using Newtonsoft.Json;

namespace DaemonMasterCore
{
    public static class SystemManagement
    {
        public static bool ValidateUser(string username, SecureString password)
        {
            if (String.IsNullOrWhiteSpace(username) || password == null)
                throw new ArgumentNullException();

            try
            {
                using (TokenHandle tokenHandle = TokenHandle.GetTokenFromLogon(username, password, NativeMethods.LOGON_TYP.Interactive))
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
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static ServiceStartInfo ParseDmdfFile(string path)
        {
            using (StreamReader streamReader = File.OpenText(path))
            {
                JsonSerializer serializer = new JsonSerializer();
                return (ServiceStartInfo)serializer.Deserialize(streamReader, typeof(ServiceStartInfo));
            }
        }
    }
}
