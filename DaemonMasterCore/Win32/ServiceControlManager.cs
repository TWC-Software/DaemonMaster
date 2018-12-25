///////////////////////////////////////////////////////////////////////////////////////// 
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
using DaemonMasterCore.Win32.PInvoke.Advapi32;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32
{
    public class ServiceControlManager : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly string DmServiceExe = AppDomain.CurrentDomain.BaseDirectory + "DaemonMasterService.exe" + " service";

        private ServiceControlManager() : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Advapi32.CloseServiceHandle(handle);
        }

        /// <summary>
        /// Creates a new service control manager instance
        /// </summary>
        /// <param name="access">The disired access rights</param>
        /// <returns>ServiceControlManager instance</returns>
        public static ServiceControlManager Connect(Advapi32.ServiceControlManagerAccessRights access)
        {
            ServiceControlManager handle = Advapi32.OpenSCManager(machineName: null, databaseName: null, access);

            if (handle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return handle;
        }

        /// <summary>
        /// Creates a new service and return his handle
        /// </summary>
        /// <param name="serviceDefinition">The services definitions</param>
        /// <returns>A ServiceHandle instance</returns>
        public ServiceHandle CreateService(IWin32ServiceDefinition serviceDefinition)
        {
            IntPtr passwordHandle = IntPtr.Zero;

            try
            {
                var serviceType = Advapi32.ServiceType.Win32OwnProcess; //DM only supports Win32OwnProcess
                if (Equals(serviceDefinition.Credentials, ServiceCredentials.LocalSystem) && serviceDefinition.CanInteractWithDesktop && !DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
                {
                    if (DaemonMasterUtils.IsSupportedWindows10VersionOrLower())
                    {
                        serviceType |= Advapi32.ServiceType.InteractivProcess;
                    }
                    else
                    {
                        throw new ArgumentException("Can interact with desktop is currently not supported in your windows version.");
                    }
                }

                //The credentials can't be null
                if (serviceDefinition.Credentials == null)
                    throw new ArgumentNullException(nameof(serviceDefinition));

                //Only call marshal if a password is set (SecureString != null), otherwise leave IntPtr.Zero
                if (serviceDefinition.Credentials.Password != null)
                    passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(serviceDefinition.Credentials.Password);

                ServiceHandle serviceHandle = Advapi32.CreateService
                (
                    this,
                    serviceDefinition.ServiceName,
                    serviceDefinition.DisplayName,
                    Advapi32.ServiceAccessRights.AllAccess,
                    serviceType,
                    serviceDefinition.StartType,
                    serviceDefinition.ErrorControl,
                    DmServiceExe,
                    loadOrderGroup: null,
                    tagId: 0, //Tags are only evaluated for driver services that have SERVICE_BOOT_START or SERVICE_SYSTEM_START start types.
                    Advapi32.ConvertDependenciesArraysToDoubleNullTerminatedString(serviceDefinition.DependOnService, serviceDefinition.DependOnGroup),
                    serviceDefinition.Credentials.Username,
                    passwordHandle
                );

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

        /// <summary>
        /// Create a ServiceHandle instance
        /// </summary>
        /// <param name="serviceName">The disired service name</param>
        /// <param name="desiredAccess">The disired access rights</param>
        /// <returns>ServiceHandle instance</returns>
        public ServiceHandle OpenService(string serviceName, Advapi32.ServiceAccessRights desiredAccess)
        {
            ServiceHandle serviceHandle = Advapi32.OpenService
            (
                this,
                serviceName,
                desiredAccess
            );

            if (serviceHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return serviceHandle;
        }
    }
}
