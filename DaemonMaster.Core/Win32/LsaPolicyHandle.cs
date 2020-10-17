/////////////////////////////////////////////////////////////////////////////////////////
//  Original Author: ROEL VAN LISDONK
//  Modified by MCPC10
//  Link to his site: https://www.roelvanlisdonk.nl/2010/03/24/how-to-grant-log-on-as-a-service-rights-to-an-user-account-using-powershell/
//  02.09.2018
/////////////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: LsaHandle
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

using DaemonMaster.Core.Win32.PInvoke;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Core.Win32.PInvoke.Kernel32;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DaemonMaster.Core.Win32
{
    /// <inheritdoc />
    /// <summary>
    /// This class is used to grant/remove/enumerate Lsa account rights.
    /// to a user.
    /// </summary>
    public class LsaPolicyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:DaemonMaster.Core.Win32.LsaPolicyHandle" /> class.
        /// </summary>
        public LsaPolicyHandle() : base(ownsHandle: true)
        {
        }

        /// <summary>
        /// Opens a new policy handle.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static LsaPolicyHandle OpenPolicyHandle()
        {
            var systemName = new Advapi32.LsaUnicodeString();

            var lsaObjectAttributes = new LsaObjectAttributes
            {
                RootDirectory = IntPtr.Zero,
                Attributes = 0,
                SecurityDescriptor = IntPtr.Zero,
                SecurityQualityOfService = IntPtr.Zero,
                Length = Marshal.SizeOf<LsaObjectAttributes>()
            };

            //Create a new LSA policy handle
            NtStatus ret = Advapi32.LsaOpenPolicy(ref systemName, ref lsaObjectAttributes, Kernel32.AccessMask.PolicySpecificRights.PolicyAllAccess, out LsaPolicyHandle policyHandle); //systemName = null (Local System)
            if (ret != NtStatus.Success)
                throw new Win32Exception(Advapi32.LsaNtStatusToWinError(ret));

            return policyHandle;
        }

        protected override bool ReleaseHandle()
        {
            return Advapi32.LsaClose(handle) == NtStatus.Success;
        }

        /// <summary>
        /// Add a privilege to the given account.
        /// </summary>
        /// <param name="account">Account name like "Olaf" xD</param>
        /// <param name="privilege"></param>
        public void AddPrivilege(string account, string privilege)
        {
            AddAccountRights(account, new[] {privilege.ToLsaString()});
        }

        /// <summary>
        /// Adds privileges to the given account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="privileges">The privileges.</param>
        public void AddPrivileges(string account, string[] privileges)
        {
            var lsaPrivileges = new Advapi32.LsaUnicodeString[privileges.Length];
            for (var i = 0; i < privileges.Length; i++)
            {
                lsaPrivileges[i] = privileges[i].ToLsaString();
            }

            AddAccountRights(account, lsaPrivileges);
        }

        /// <summary>
        /// Remove a privilege from the given account
        /// </summary>
        /// <param name="account">Account name like "Olaf"</param>
        /// <param name="privilege"></param>
        /// <param name="removeAllRights">Remove all privileges</param>
        public void RemovePrivilege(string account, string privilege, bool removeAllRights = false)
        {
            RemoveAccountRights(account, new []{privilege.ToLsaString()}, removeAllRights);
        }

        /// <summary>
        /// Removes the privileges from the given account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="privileges">The privileges.</param>
        /// <param name="removeAllRights">if set to <c>true</c> [remove all rights].</param>
        public void RemovePrivileges(string account, string[] privileges, bool removeAllRights = false)
        {
            var lsaPrivileges = new Advapi32.LsaUnicodeString[privileges.Length];
            for (var i = 0; i < privileges.Length; i++)
            {
                lsaPrivileges[i] = privileges[i].ToLsaString();
            }

            RemoveAccountRights(account, lsaPrivileges, removeAllRights);
        }

        /// <summary>
        /// Give you an array with all privileges that the account have.
        /// </summary>
        /// <param name="account">Account name like "Olaf"</param>
        /// <returns></returns>
        public string[] EnumeratePrivileges(string account)
        {
            IntPtr rightsPtr = IntPtr.Zero;
            try
            {
                uint countOfRights;
                using (var win32Sid = new Win32Sid(account))
                {
                    //Enumerate account rights
                    NtStatus ret = Advapi32.LsaEnumerateAccountRights(this, win32Sid.Pointer, out rightsPtr, out countOfRights);

                    if (ret == NtStatus.ObjectNameNotFound) //When you use a user account that does not have privileges explicitly assigned to it, the function will return NtStatus.ObjectNameNotFound.
                        return Array.Empty<string>();

                    if (ret != NtStatus.Success)
                        throw new Win32Exception(Advapi32.LsaNtStatusToWinError(ret));
                }

                var lsaUnicodeStringSize = Marshal.SizeOf<Advapi32.LsaUnicodeString>();
                var privileges = new string[countOfRights];
                IntPtr tempPtr = rightsPtr;
                for (var i = 0; i < countOfRights; i++)
                {
                    var lasPrivilege = Marshal.PtrToStructure<Advapi32.LsaUnicodeString>(tempPtr);
                    IntPtr.Add(tempPtr, lsaUnicodeStringSize);

                    privileges[i] = lasPrivilege.Buffer;
                }

                return privileges;
            }
            finally
            {
                if (rightsPtr != IntPtr.Zero)
                    Advapi32.LsaFreeMemory(rightsPtr);
            }
        }


        private void AddAccountRights(string account, Advapi32.LsaUnicodeString[] rights)
        {
            using (var win32Sid = new Win32Sid(account))
            {
                //Add account rights
                NtStatus ret = Advapi32.LsaAddAccountRights(this, win32Sid.Pointer, rights, 1);
                if (ret != NtStatus.Success)
                    throw new Win32Exception(Advapi32.LsaNtStatusToWinError(ret));
            }
        }

        private void RemoveAccountRights(string account, Advapi32.LsaUnicodeString[] rights, bool removeAllRights = false)
        {
            using (var win32Sid = new Win32Sid(account))
            {
                //Remove account rights
                NtStatus ret = Advapi32.LsaRemoveAccountRights(this, win32Sid.Pointer, removeAllRights, rights, 1);
                if (ret != NtStatus.Success)
                    throw new Win32Exception(Advapi32.LsaNtStatusToWinError(ret));
            }
        }
    }
}
