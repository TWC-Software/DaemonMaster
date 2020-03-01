using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DaemonMaster.Core.Exceptions;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using Microsoft.Win32.SafeHandles;

namespace DaemonMaster.Core.Win32
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

        /// <summary>
        /// Start the service
        /// </summary>
        public void Start()
        {
            Start(Array.Empty<string>());
        }

        /// <summary>
        /// Start the service with the given arguments
        /// </summary>
        /// <param name="arguments"></param>
        public void Start(string[] arguments)
        {
            if (!Advapi32.StartService(serviceHandle: this, numberOfArgs: (uint)arguments.Length, args: arguments))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public void Stop()
        {
            var serviceStatus = new Advapi32.ServiceStatus();
            if (!Advapi32.ControlService(serviceHandle: this, Advapi32.ServiceControl.Stop, ref serviceStatus))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

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
            //Change the config
            ChangeConfig
            (
                serviceDefinition.DisplayName,
                serviceDefinition.StartType,
                serviceDefinition.CanInteractWithDesktop,
                serviceDefinition.LoadOrderGroup,
                serviceDefinition.ErrorControl,
                serviceDefinition.Credentials,
                serviceDefinition.DependOnService,
                serviceDefinition.DependOnGroup
            );

            //Set the description
            ChangeDescription(serviceDefinition.Description);

            //Set delayed start
            ChangeDelayedStart(serviceDefinition.DelayedStart);

            //Set the failure actions on non crash failures
            ChangeFailureActionsOnNonCrashFailures(serviceDefinition.FailureActionsOnNonCrashFailures);
        }

        /// <summary>
        /// Changes the service configuration.
        /// </summary>
        /// <param name="displayName">The display name.</param>
        /// <param name="startType">The start type.</param>
        /// <param name="canInteractWithDesktop">if set to <c>true</c> the service can interact with the desktop.</param>
        /// <param name="loadOrderGroup">The load order group.</param>
        /// <param name="errorControl">The error control.</param>
        /// <param name="credentials">The credentials.</param>
        /// <param name="dependOnService">The services on which the service depend on.</param>
        /// <param name="dependOnGroup">The groups on which the service depend on.</param>
        /// <exception cref="System.ArgumentException">Can interact with desktop is currently not supported in your windows version.</exception>
        /// <exception cref="System.ArgumentNullException">credentials</exception>
        /// <exception cref="Win32Exception"></exception>
        public void ChangeConfig
        (
            string displayName,
            Advapi32.ServiceStartType startType,
            bool canInteractWithDesktop,
            string loadOrderGroup,
            Advapi32.ErrorControl errorControl,
            ServiceCredentials credentials,
            string[] dependOnService,
            string[] dependOnGroup
         )
        {
            if (QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                throw new ServiceNotStoppedException();

            var serviceType = Advapi32.ServiceType.Win32OwnProcess; //DM only supports Win32OwnProcess
            if (Equals(credentials, ServiceCredentials.LocalSystem) && canInteractWithDesktop && !DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
            {
                if (DaemonMasterUtils.IsSupportedWindows10VersionForIwd)
                {
                    serviceType |= Advapi32.ServiceType.InteractivProcess;
                }
                else
                {
                    throw new ArgumentException("Can interact with desktop is currently not supported in your windows version.");
                }
            }

            //The credentials can't be null
            if (credentials == null)
                throw new ArgumentNullException(nameof(credentials));

            IntPtr passwordHandle = IntPtr.Zero;
            try
            {
                //Only call marshal if a password is set (SecureString != null), otherwise leave IntPtr.Zero
                if (credentials.Password != null)
                    passwordHandle = Marshal.SecureStringToGlobalAllocUnicode(credentials.Password);

                bool result = Advapi32.ChangeServiceConfig
                (
                    this,
                    serviceType,
                    startType,
                    errorControl,
                    ServiceControlManager.DmServiceExe,
                    loadOrderGroup,
                    tagId: 0, // Tags are only evaluated for driver services that have SERVICE_BOOT_START or SERVICE_SYSTEM_START start types.
                    Advapi32.ConvertDependenciesArraysToDoubleNullTerminatedString(dependOnService, dependOnGroup),
                    credentials.Username,
                    passwordHandle,
                    displayName
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
        /// <exception cref="ArgumentException"></exception>
        public void ChangeDescription(string description)
        {
            if (QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                throw new ServiceNotStoppedException();

            if (description == null)
                throw new ArgumentNullException(nameof(description));

            IntPtr ptr = IntPtr.Zero;

            //Create the struct
            Advapi32.ServiceConfigDescription serviceDescription;
            serviceDescription.description = description;

            try
            {
                // Copy the struct to unmanaged memory
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(serviceDescription));
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
        /// <param name="enable">
        /// When <c>true</c>, the service start delayed.
        /// When <c>false</c>, the service start normal.
        /// </param>
        public void ChangeDelayedStart(bool enable)
        {
            if (QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                throw new ServiceNotStoppedException();

            IntPtr ptr = IntPtr.Zero;

            //Create the struct
            Advapi32.ServiceConfigDelayedAutoStartInfo serviceDelayedAutoStartInfo;
            serviceDelayedAutoStartInfo.DelayedAutostart = enable;

            try
            {
                // Copy the struct to unmanaged memory
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(serviceDelayedAutoStartInfo));
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

        /// <summary>
        /// Allow failure actions on non crash failures.
        /// The change takes effect the next time the system is started.
        /// </summary>
        /// <param name="enable">
        /// When <c>true</c>, failure actions will be called even when the service reports that it is stopped but with a return code diffrent from zero.
        /// When <c>false</c>, failure actions will only be called when the service terminates without reporting an exit code.
        /// </param>
        /// <exception cref="Win32Exception"></exception>
        public void ChangeFailureActionsOnNonCrashFailures(bool enable)
        {
            if (QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                throw new ServiceNotStoppedException();

            IntPtr ptr = IntPtr.Zero;

            //Create the struct
            Advapi32.ServiceConfigFailureActionsFlag serviceConfigFailureActionsFlag;
            serviceConfigFailureActionsFlag.failureActionsOnNonCrashFailures = enable;

            try
            {
                // Copy the struct to unmanaged memory
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(serviceConfigFailureActionsFlag));
                Marshal.StructureToPtr(serviceConfigFailureActionsFlag, ptr, fDeleteOld: false);

                //Call ChangeServiceConfig2
                if (!Advapi32.ChangeServiceConfig2(this, Advapi32.ServiceInfoLevel.FailureActionsFlag, ptr))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            }
        }

        /// <summary>
        /// Changes the failure actions of the service.
        /// </summary>
        /// <param name="failureActions">The failure actions.</param>
        /// <exception cref="Win32Exception"></exception>
        public void ChangeFailureActions(Advapi32.ServiceFailureActions failureActions)
        {
            if (QueryServiceStatus().currentState != Advapi32.ServiceCurrentState.Stopped)
                throw new ServiceNotStoppedException();

            IntPtr ptr = IntPtr.Zero;

            //Create the unmanaged struct
            Advapi32.ServiceConfigFailureActions serviceConfigFailureActions;
            serviceConfigFailureActions.resetPeriode = failureActions.ResetPeriode;
            serviceConfigFailureActions.rebootMessage = failureActions.RebootMessage;
            serviceConfigFailureActions.command = failureActions.Command;
            serviceConfigFailureActions.actionsLength = (uint)failureActions.Actions.Count;
            serviceConfigFailureActions.actions = failureActions.Actions.ToArray();

            try
            {
                // Copy the struct to unmanaged memory
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(serviceConfigFailureActions));
                Marshal.StructureToPtr(serviceConfigFailureActions, ptr, fDeleteOld: false);

                //Call ChangeServiceConfig2
                if (!Advapi32.ChangeServiceConfig2(this, Advapi32.ServiceInfoLevel.FailureActions, ptr))
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
        /// Gets the service pid. If the process id is invalid the method returns the default value of the type (in this case: null).
        /// </summary>
        /// <returns></returns>
        public uint GetServicePid()
        {
            return IsClosed ? 0 : QueryServiceStatus().processId;
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
                if (!Advapi32.QueryServiceStatusEx(serviceHandle: this, Advapi32.ScStatusProcessInfo, buffer: IntPtr.Zero, 0, out uint bytesNeeded))
                {
                    int result = Marshal.GetLastWin32Error();

                    if (result != 0x7A) //ERROR_INSUFFICIENT_BUFFER
                        throw new Win32Exception(result);
                }

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

        /// <summary>
        /// Executes the command. Needs SERVICE_USER_DEFINED_CONTROL right.
        /// </summary>
        /// <param name="command">The command.</param>
        public void ExecuteCommand(int command)
        {
            if (command < 128 || command > 255)
                throw new ArgumentException("Only a range of 128-255 is allowed.");

            var serviceStatus = new Advapi32.ServiceStatus();
            Advapi32.ControlService(this, command, ref serviceStatus);
        }

        /// <summary>
        /// Waits for status.
        /// </summary>
        /// <param name="desiredStatus">The desired status.</param>
        public void WaitForStatus(Advapi32.ServiceCurrentState desiredStatus)
        {
            WaitForStatus(desiredStatus, TimeSpan.MaxValue);
        }

        /// <summary>
        /// Waits for the given status the given time.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="timeout">The timeout.</param>
        /// <exception cref="TimeoutException">WaitForStatus</exception>
        public void WaitForStatus(Advapi32.ServiceCurrentState desiredStatus, TimeSpan timeout)
        {
            Advapi32.ServiceCurrentState serviceCurrentState = QueryServiceStatus().currentState;

            DateTime utcNow = DateTime.UtcNow;
            while (serviceCurrentState != desiredStatus)
            {
                if (DateTime.UtcNow - utcNow > timeout)
                    throw new TimeoutException("Service: WaitForStatus timeout.");

                Thread.Sleep(250);
                serviceCurrentState = QueryServiceStatus().currentState;
            }
        }
    }
}
