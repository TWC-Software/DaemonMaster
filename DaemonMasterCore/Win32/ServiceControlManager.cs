/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceControlManager
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
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32
{
    public class ServiceControlManager : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ServiceControlManager() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }

        //Give a ServiceControlManager object as return value 
        public static ServiceControlManager Connect(NativeMethods.SCM_ACCESS access)
        {
            ServiceControlManager handle = NativeMethods.OpenSCManager(null, null, access);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return handle;
        }

        //Create a new Service and return the ServiceHandle 
        public ServiceHandle CreateService(
            string serviceName,
            string displayName,
            NativeMethods.SERVICE_ACCESS desiredAccess,
            uint serviceType,
            NativeMethods.SERVICE_START startType,
            NativeMethods.SERVICE_ERROR_CONTROL errorControl,
            string binaryPathName,
            string loadOrderGroup,
            string tagId,
            StringBuilder dependencies,
            string serviceStartName,
            SecureString password)
        {
            IntPtr passwordHandle = IntPtr.Zero;

            try
            {
                if (password != null)
                    passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);


                ServiceHandle serviceHandle = NativeMethods.CreateService(this, serviceName, displayName,
                    desiredAccess,
                    serviceType, startType, errorControl, binaryPathName, loadOrderGroup, tagId, dependencies,
                    serviceStartName, passwordHandle);

                if (serviceHandle.IsInvalid)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return serviceHandle;
            }
            finally
            {
                if (passwordHandle != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordHandle);
            }
        }

        //Open a service and return the ServiceHandle
        public ServiceHandle OpenService(string serviceName, NativeMethods.SERVICE_ACCESS desiredAccess)
        {
            ServiceHandle serviceHandle = NativeMethods.OpenService(this, serviceName, desiredAccess);

            if (serviceHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return serviceHandle;
        }
    }
}
