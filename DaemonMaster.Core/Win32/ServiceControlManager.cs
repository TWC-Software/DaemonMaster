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
using System.Linq;
using System.Runtime.InteropServices;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using Microsoft.Win32.SafeHandles;

namespace DaemonMaster.Core.Win32
{
    public class ServiceControlManager : SafeHandleZeroOrMinusOneIsInvalid
    {
        public static readonly string DmServiceExe = AppDomain.CurrentDomain.BaseDirectory + "DaemonMasterService.exe" + " service";

        private ServiceControlManager() : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Advapi32.CloseServiceHandle(handle);
        }

        public static bool ValidServiceName(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > 80)
                return false;

            return serviceName.ToCharArray().All(c => c != '\\' && c != '/');
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

            if (!ValidServiceName(serviceDefinition.ServiceName))
                throw new ArgumentException("The given service name is not a valid name.");

            //Create the service type
            var serviceType = Advapi32.ServiceType.Win32OwnProcess; //DM only supports Win32OwnProcess
            if (Equals(serviceDefinition.Credentials, ServiceCredentials.LocalSystem) && serviceDefinition.CanInteractWithDesktop)
            {
                if (DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
                {
                    serviceType |= Advapi32.ServiceType.InteractivProcess;
                }
                else
                {
                    throw new ArgumentException("Can interact with desktop is not supported by your windows version.");
                }
            }

            //The credentials can't be null
            if (serviceDefinition.Credentials == null)
                throw new ArgumentNullException(nameof(serviceDefinition.Credentials));

            try
            {
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
                    DmServiceExe.SurroundWithDoubleQuotes(),
                    serviceDefinition.LoadOrderGroup,
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

        /// <summary>
        /// Gets the name of the service from the display name.
        /// </summary>
        /// <param name="displayName">The display name of the service.</param>
        /// <returns></returns>
        /// <exception cref="Win32Exception">
        /// </exception>
        public string GetServiceName(string displayName)
        {
            IntPtr bufferPtr = IntPtr.Zero;
            uint bytesNeeded = 0;

            try
            {
                //Determine the required buffer size => buffer and bufferSize must be null
                if (!Advapi32.GetServiceKeyName(this, displayName, IntPtr.Zero, ref bytesNeeded))
                {
                    int result = Marshal.GetLastWin32Error();

                    if (result != 0x7A) //ERROR_INSUFFICIENT_BUFFER
                        throw new Win32Exception(result);
                }

                //+1 for NULL terminator
                bytesNeeded++;

                //Allocate the required buffer size
                bufferPtr = Marshal.AllocHGlobal((int)bytesNeeded); 


                if (!Advapi32.GetServiceKeyName(this, displayName, bufferPtr, ref bytesNeeded))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return Marshal.PtrToStringUni(bufferPtr, (int)bytesNeeded);
            }
            finally
            {
                if (bufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }
}
