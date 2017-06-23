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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32
{
    public class ServiceControlManager : SafeHandle
    {
        public ServiceControlManager() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        //Give a ServiceControlManager object as return value 
        public static ServiceControlManager Connect(NativeMethods.SCM_ACCESS access)
        {
            ServiceControlManager handle = NativeMethods.OpenSCManager(null, null, (uint)access);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return handle;
        }

        //Create a new Service and return the ServiceHandle 
        public ServiceHandle CreateService(
            string serviceName,
            string displayName,
            NativeMethods.SERVICE_ACCESS desiredAccess,
            NativeMethods.SERVICE_TYPE serviceType,
            NativeMethods.SERVICE_START startType,
            NativeMethods.SERVICE_ERROR_CONTROLE errorControl,
            string binaryPathName,
            string loadOrderGroup,
            string tagId,
            string dependencies,
            string serviceStartName,
            string password)
        {
            ServiceHandle serviceHandle = NativeMethods.CreateService(this, serviceName, displayName, (uint)desiredAccess,
                (uint)serviceType, (uint)startType, (uint)errorControl, binaryPathName, loadOrderGroup, tagId, dependencies,
                serviceStartName, password);

            if (serviceHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return serviceHandle;
        }

        //Open a service and return the ServiceHandle
        public ServiceHandle OpenService(string serviceName, NativeMethods.SERVICE_ACCESS desiredAccess)
        {
            ServiceHandle serviceHandle = NativeMethods.OpenService(this, serviceName, (uint)desiredAccess);

            if (serviceHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return serviceHandle;
        }
    }
}
