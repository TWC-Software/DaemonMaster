//  DaemonMaster: ServiceHandle
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
    public class ServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ServiceHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }

        public void Start()
        {
            if (!NativeMethods.StartService(this, 0, null))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void Stop()
        {
            NativeMethods.SERVICE_STATUS serviceStatus = new NativeMethods.SERVICE_STATUS();
            if (!NativeMethods.ControlService(this, NativeMethods.SERVICE_CONTROL.STOP, ref serviceStatus))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void DeleteService()
        {
            if (!NativeMethods.DeleteService(this))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void SetDescription(string description)
        {
            //Create an struct with description of the service
            NativeMethods.SERVICE_DESCRIPTION serviceDescription;
            serviceDescription.lpDescription = description;

            //Set the description of the service
            if (!NativeMethods.ChangeServiceConfig2(this, NativeMethods.INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION, ref serviceDescription))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void SetDelayedStart(bool enable)
        {
            // //Create an struct with description of the service
            NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO serviceDelayedStart;
            serviceDelayedStart.delayedStart = enable;

            //Set the description of the service
            if (!NativeMethods.ChangeServiceConfig2(this, NativeMethods.INFO_LEVEL.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                ref serviceDelayedStart))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void ChangeConfig(NativeMethods.SERVICE_START startType, string displayName, StringBuilder dependencies, string username, SecureString password)
        {
            IntPtr passwordHandle = IntPtr.Zero;

            try
            {
                if (password != null)
                    passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(password);

                if (!NativeMethods.ChangeServiceConfig(this, NativeMethods.SERVICE_TYPE.SERVICE_NO_CHANGE, startType,
                    NativeMethods.SERVICE_ERROR_CONTROL.SERVICE_NO_CHANGE, null, null, null, dependencies, username,
                    passwordHandle, displayName))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (passwordHandle != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordHandle);
            }
        }

        //http://www.pinvoke.net/default.aspx/advapi32.QueryServiceStatusEx
        public NativeMethods.SERVICE_STATUS_PROCESS QueryServiceStatusEx()
        {
            IntPtr buffer = IntPtr.Zero;
            int size = 0;

            try
            {
                NativeMethods.QueryServiceStatusEx(this, NativeMethods.SC_STATUS_PROCESS_INFO, buffer, size, out size);
                //Reserviere Speicher in der gr��e von size
                buffer = Marshal.AllocHGlobal(size);

                if (!NativeMethods.QueryServiceStatusEx(this, NativeMethods.SC_STATUS_PROCESS_INFO, buffer, size, out size))
                    throw new Win32Exception(Marshal.GetLastWin32Error());


                return (NativeMethods.SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(buffer, typeof(NativeMethods.SERVICE_STATUS_PROCESS));
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
