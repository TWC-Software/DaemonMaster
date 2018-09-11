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

using System;
using System.Security.Principal;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    /// <summary>
    /// This class is used to grant/remove/emumerate Lsa account rights.
    /// to a user.
    /// </summary>
    public class LsaHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Open an new lsa policy instance
        /// </summary>
        /// <param name="systemName">null = local system</param>
        public LsaHandle(string systemName = null) : base(true)
        {
            NativeMethods.LSA_OBJECT_ATTRIBUTES lsaAttr;
            lsaAttr.RootDirectory = IntPtr.Zero;
            lsaAttr.ObjectName = IntPtr.Zero;
            lsaAttr.Attributes = 0;
            lsaAttr.SecurityDescriptor = IntPtr.Zero;
            lsaAttr.SecurityQualityOfService = IntPtr.Zero;
            lsaAttr.Length = Marshal.SizeOf(typeof(NativeMethods.LSA_OBJECT_ATTRIBUTES));


            NativeMethods.LSA_UNICODE_STRING[] system = null;
            if (systemName != null)
            {
                system = new NativeMethods.LSA_UNICODE_STRING[1];
                system[0] = InitLsaString(systemName);
            }

            //Create a LSA policy handel
            uint ret = NativeMethods.LsaOpenPolicy(system, ref lsaAttr, (int)NativeMethods.ACCESS.POLICY_ALL_ACCESS, ref handle);
            if (ret != NativeMethods.STATUS_SUCCESS)
                throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ret));
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.LsaClose(handle) == NativeMethods.STATUS_SUCCESS;
        }

        /// <summary>
        /// Add privileges to the given account
        /// </summary>
        /// <param name="account">Account name like "Olaf"</param>
        /// <param name="privilege"></param>
        public void AddPrivileges(string account, string[] privilege)
        {
            if (String.IsNullOrWhiteSpace(account) || privilege == null)
                return;


            IntPtr pSid = new Sid(account).Pointer;

            NativeMethods.LSA_UNICODE_STRING[] privileges = new NativeMethods.LSA_UNICODE_STRING[privilege.Length];
            for (int i = 0; i < privilege.Length; i++)
            {
                privileges[i] = InitLsaString(privilege[i]);
            }

            //Add account rights
            uint ret = NativeMethods.LsaAddAccountRights(this, pSid, privileges, (uint)privilege.Length);
            if (ret != NativeMethods.STATUS_SUCCESS)
                throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ret));
        }

        /// <summary>
        /// Remove privileges from the given account
        /// </summary>
        /// <param name="account">Account name like "Olaf"</param>
        /// <param name="privilege"></param>
        /// <param name="removeAllRights">Remove all privileges</param>
        public void RemovePrivileges(string account, string[] privilege, bool removeAllRights = false)
        {
            if (String.IsNullOrWhiteSpace(account) || privilege == null)
                return;


            IntPtr pSid = new Sid(account).Pointer;

            NativeMethods.LSA_UNICODE_STRING[] privileges = new NativeMethods.LSA_UNICODE_STRING[privilege.Length];
            for (int i = 0; i < privilege.Length; i++)
            {
                privileges[i] = InitLsaString(privilege[i]);
            }

            //Remove account rights
            uint ret = NativeMethods.LsaRemoveAccountRights(this, pSid, removeAllRights, privileges, (uint)privilege.Length);
            if (ret != NativeMethods.STATUS_SUCCESS)
                throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ret));
        }

        /// <summary>
        /// Give you an array with all privileges that the account have
        /// </summary>
        /// <param name="account">Account name like "Olaf"</param>
        /// <returns></returns>
        public NativeMethods.LSA_UNICODE_STRING[] EnumeratePrivileges(string account)
        {
            if (String.IsNullOrWhiteSpace(account))
                throw new ArgumentException("String is empty or null!");


            IntPtr pSid = new Sid(account).Pointer;
            IntPtr rightsPtr = IntPtr.Zero;

            try
            {
                //Remove account rights
                uint ret = NativeMethods.LsaEnumerateAccountRights(this, pSid, ref rightsPtr, out uint countOfRights);
                if (ret != NativeMethods.STATUS_SUCCESS)
                    throw new Win32Exception(NativeMethods.LsaNtStatusToWinError(ret));

                NativeMethods.LSA_UNICODE_STRING[] privileges = new NativeMethods.LSA_UNICODE_STRING[countOfRights];
                IntPtr ptr = rightsPtr;
                for (int i = 0; i < countOfRights; i++)
                {
                    privileges[i] = (NativeMethods.LSA_UNICODE_STRING)Marshal.PtrToStructure(ptr, typeof(NativeMethods.LSA_UNICODE_STRING));
                    ptr = ptr + Marshal.SizeOf(typeof(NativeMethods.LSA_UNICODE_STRING));
                }

                return privileges;
            }
            finally
            {
                if (rightsPtr != IntPtr.Zero)
                    NativeMethods.LsaFreeMemory(rightsPtr);
            }
        }

        // helper functions

        /// <summary>
        /// Create an new LsaString from a string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static NativeMethods.LSA_UNICODE_STRING InitLsaString(string s)
        {
            if (String.IsNullOrWhiteSpace(s))
                throw new ArgumentException("String is empty or null!");

            // Unicode strings max. 32KB
            if (s.Length > 0x7ffe)
                throw new ArgumentException("String is too long");

            NativeMethods.LSA_UNICODE_STRING lus = new NativeMethods.LSA_UNICODE_STRING
            {
                Buffer = s,
                Length = (ushort)(s.Length * sizeof(char)),
                MaximumLength = (ushort)((s.Length + 1) * sizeof(char))
            };

            return lus;
        }
    }

    /// <summary>
    /// Class that read the Sid infos and give them as pointer
    /// </summary>
    sealed class Sid : IDisposable
    {
        public IntPtr Pointer { get; private set; } = IntPtr.Zero;

        public Sid(string account)
        {
            SecurityIdentifier sid = (SecurityIdentifier)(new NTAccount(account)).Translate(typeof(SecurityIdentifier));
            Byte[] buffer = new Byte[sid.BinaryLength];
            sid.GetBinaryForm(buffer, 0);

            Pointer = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, Pointer, buffer.Length);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            if (Pointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Pointer);
            }
        }

        ~Sid()
        {
            ReleaseUnmanagedResources();
        }
    }
}
