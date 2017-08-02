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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using DaemonMasterCore.Win32.PInvoke;
using System;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security;

namespace DaemonMasterCore
{
    public static class SystemManagement
    {
        [Obsolete]
        public static bool ValidateUser(string username, SecureString password)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                return pc.ValidateCredentials(username, SecurityManagement.ConvertSecureStringToString(password));
            }
        }

        public static bool ValidateUserWin32(string username, SecureString password)
        {
            if (String.IsNullOrWhiteSpace(username) || password == null)
                throw new ArgumentNullException();

            IntPtr tokenHandle = IntPtr.Zero;
            IntPtr passwordPtr = IntPtr.Zero;

            try
            {
                passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
                return NativeMethods.LogonUser(username, ".", passwordPtr, NativeMethods.LOGON_TYP.Interactive, NativeMethods.LOGON_PROVIDER.Default, out tokenHandle);
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);

                if (tokenHandle != IntPtr.Zero)
                    NativeMethods.CloseHandle(tokenHandle);
            }
        }
    }
}
