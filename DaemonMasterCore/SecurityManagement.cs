/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SecurityManagement
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
using System.Runtime.InteropServices;
using System.Security;

namespace DaemonMasterCore
{
    public static class SecurityManagement
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Security                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Security

        /// <summary>
        /// Convert the given string to a SecureString
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static SecureString ConvertStringToSecureString(this string data)
        {
            if (String.IsNullOrWhiteSpace(data))
                return null;

            SecureString secString = new SecureString();

            if (data.Length > 0)
            {
                foreach (char c in data)
                {
                    secString.AppendChar(c);
                }
                return secString;
            }
            return null;
        }

        /// <summary>
        /// Convert the given SecureString to a normal string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ConvertSecureStringToString(this SecureString data)
        {
            if (data == null)
                return null;

            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(data);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
        #endregion
    }
}
