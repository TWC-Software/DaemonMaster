using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using DaemonMasterCore.Win32.PInvoke.Advapi32;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32
{
    /// <inheritdoc />
    /// <summary>
    /// This class represents a service handle and allow you to change the config, start, etc...
    /// </summary>
    public class ServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public ServiceHandle() : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Advapi32.CloseServiceHandle(handle);
        }

        ///// <summary>
        ///// Start the service
        ///// </summary>
        //public void Start()
        //{
        //    Start(Array.Empty<string>());
        //}

        ///// <summary>
        ///// Start the service with the given arguments
        ///// </summary>
        ///// <param name="arguments"></param>
        //public void Start(string[] arguments)
        //{
        //    if (!Advapi32.StartService(serviceHandle: this, numberOfArgs: (uint)arguments.Length, args: arguments))
        //        throw new Win32Exception(Marshal.GetLastWin32Error());
        //}

        ///// <summary>
        ///// Stop the service
        ///// </summary>
        //public void Stop()
        //{
        //    var serviceStatus = new Advapi32.ServiceStatus();
        //    if (!Advapi32.ControlService(serviceHandle: this, Advapi32.ServiceControl.Stop, ref serviceStatus))
        //        throw new Win32Exception(Marshal.GetLastWin32Error());
        //}

        /// <summary>
        /// Delete the service
        /// </summary>
        public void DeleteService()
        {
            if (!Advapi32.DeleteService(serviceHandle: this))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Allows you to change the service config
        /// </summary>
        /// <param name="serviceDefinition">Service definition instance with all paramaters</param>
        public void ChangeConfig(IWin32ServiceDefinition serviceDefinition)
        {
            IntPtr passwordHandle = IntPtr.Zero;


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


            //Set the description
            ChangeDescription(serviceDefinition.Description);

            //Set delayed start
            ChangeDelayedStart(serviceDefinition.DelayedStart);

            try
            {
                //Only call marshal if a password is set (SecureString != null), otherwise leave IntPtr.Zero
                if (serviceDefinition.Credentials.Password != null)
                    passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(serviceDefinition.Credentials.Password);

                bool result = Advapi32.ChangeServiceConfig
                (
                    this,
                    serviceType,
                    serviceDefinition.StartType,
                    serviceDefinition.ErrorControl,
                    ServiceControlManager.DmServiceExe,
                    loadOrderGroup: null,
                    tagId: 0, // Tags are only evaluated for driver services that have SERVICE_BOOT_START or SERVICE_SYSTEM_START start types.
                    Advapi32.ConvertDependenciesArraysToDoubleNullTerminatedString(serviceDefinition.DependOnService, serviceDefinition.DependOnGroup),
                    serviceDefinition.Credentials.Username,
                    passwordHandle,
                    serviceDefinition.DisplayName
                );

                if (!result)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (passwordHandle != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordHandle);
            }
        }

        /// <summary>
        /// Allows you to set change the description text
        /// </summary>
        /// <param name="description">Description text</param>
        private void ChangeDescription(string description)
        {
            IntPtr ptr = IntPtr.Zero;

            //Create the struct
            Advapi32.ServiceDescription serviceDescription;
            serviceDescription.description = description;

            try
            {
                // Copy the struct to unmanaged memory
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Advapi32.ServiceDescription>());
                Marshal.StructureToPtr(serviceDescription, ptr, fDeleteOld: false);

                //Call ChangeServiceConfig2
                if (!Advapi32.ChangeServiceConfig2(this, Advapi32.ServiceInfoLevel.Description, ptr))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Allows you to disable or enable the delayed start of the service
        /// </summary>
        /// <param name="enable">Enable delayed start</param>
        private void ChangeDelayedStart(bool enable)
        {
            IntPtr ptr = IntPtr.Zero;

            //Create the struct
            Advapi32.ServiceDelayedAutoStartInfo serviceDelayedAutoStartInfo;
            serviceDelayedAutoStartInfo.DelayedAutostart = enable;

            try
            {
                // Copy the struct to unmanaged memory
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Advapi32.ServiceDelayedAutoStartInfo>());
                Marshal.StructureToPtr(serviceDelayedAutoStartInfo, ptr, fDeleteOld: false);

                //Call ChangeServiceConfig2
                if (!Advapi32.ChangeServiceConfig2(this, Advapi32.ServiceInfoLevel.DelayedAutoStartInfo, ptr))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        //TODO: Rest of ChangeServiceConfig2

        /// <summary>
        /// Gets the service pid.
        /// </summary>
        /// <returns></returns>
        public uint GetServicePid()
        {
            if (IsInvalid)
                return 0;

            return QueryServiceStatus().processId;
        }

        /// <summary>
        /// Query the current status of the service
        /// </summary>
        /// <returns>Service status process instance</returns>
        public Advapi32.ServiceStatusProcess QueryServiceStatus()
        {
            IntPtr bufferPtr = IntPtr.Zero;

            try
            {
                //Determine the required buffer size => buffer and bufferSize must be null
                Advapi32.QueryServiceStatusEx(serviceHandle: this, Advapi32.ScStatusProcessInfo, buffer: IntPtr.Zero, bufferSize: 0, out uint bytesNeeded);

                //Allocate the required buffer size
                bufferPtr = Marshal.AllocHGlobal((int)bytesNeeded);


                if (!Advapi32.QueryServiceStatusEx(serviceHandle: this, Advapi32.ScStatusProcessInfo, buffer: bufferPtr, bufferSize: bytesNeeded, out bytesNeeded))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return (Advapi32.ServiceStatusProcess)Marshal.PtrToStructure(bufferPtr, typeof(Advapi32.ServiceStatusProcess));
            }
            finally
            {
                if (bufferPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(bufferPtr);
            }
        }
    }
}
