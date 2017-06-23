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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32
{
    public class ServiceHandle : SafeHandle
    {
        public ServiceHandle() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseServiceHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

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
            if (!NativeMethods.ChangeServiceConfig2(this, (uint)NativeMethods.DW_INFO_LEVEL.SERVICE_CONFIG_DESCRIPTION, ref serviceDescription))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void SetDelayedStart(bool enable)
        {
            // //Create an struct with description of the service
            NativeMethods.SERVICE_CONFIG_DELAYED_AUTO_START_INFO serviceDelayedStart;
            serviceDelayedStart.delayedStart = enable;

            //Set the description of the service
            if (!NativeMethods.ChangeServiceConfig2(this, (uint)NativeMethods.DW_INFO_LEVEL.SERVICE_CONFIG_DELAYED_AUTO_START_INFO,
                ref serviceDelayedStart))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void ChangeConfig(NativeMethods.SERVICE_START startType, string displayName)
        {
            if (!NativeMethods.ChangeServiceConfig(this, NativeMethods.SERVICE_NO_CHANGE, (uint)startType,
                NativeMethods.SERVICE_NO_CHANGE, null, null, null, null/*String.Concat(daemon.DependOnService)*/, null, null, displayName))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        //http://www.pinvoke.net/default.aspx/advapi32.QueryServiceStatusEx
        public NativeMethods.SERVICE_STATUS_PROCESS QueryServiceStatusEx()
        {
            IntPtr buffer = IntPtr.Zero;
            int size = 0;

            try
            {
                NativeMethods.QueryServiceStatusEx(this, 0, buffer, size, out size);
                //Reserviere Speicher in der gr��e von size
                buffer = Marshal.AllocHGlobal(size);

                if (!NativeMethods.QueryServiceStatusEx(this, 0, buffer, size, out size))
                    throw new Win32Exception(Marshal.GetLastWin32Error());


                return (NativeMethods.SERVICE_STATUS_PROCESS)Marshal.PtrToStructure(buffer, typeof(NativeMethods.SERVICE_STATUS_PROCESS));
            }
            catch (Exception)
            {
                throw new NotImplementedException();
            }
            finally
            {
                //Gebe Speicher, wenn genutzt, wieder frei
                if (!buffer.Equals(IntPtr.Zero))
                    Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
