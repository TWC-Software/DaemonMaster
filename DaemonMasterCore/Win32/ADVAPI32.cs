using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DaemonMasterCore.Win32
{
    //FROM PINVOKE.NET
    public static partial class NativeMethods
    {
        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "CreateServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ServiceHandle CreateService
        (
            ServiceControlManager hSCManager,
            string lpServiceName,
            string lpDisplayName,
            SERVICE_ACCESS dwDesiredAccess,
            SERVICE_TYPE dwServiceType,
            SERVICE_START dwStartType,
            SERVICE_ERROR_CONTROLE dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            string lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword
        );

        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ServiceControlManager OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwAccess);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseServiceHandle(IntPtr hSCManager);

        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "StartServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool StartService(ServiceHandle hService, uint dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "OpenServiceW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern ServiceHandle OpenService(ServiceControlManager hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteService(ServiceHandle hService);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool QueryServiceStatusEx(ServiceHandle hService, DW_INFO_LEVEL infoLevel, IntPtr buffer, int bufferSize, out int bytesNeeded);

        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "ChangeServiceConfig2W", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeServiceConfig2(ServiceHandle hService, DW_INFO_LEVEL dwInfoLevel, [MarshalAs(UnmanagedType.Struct)] ref SERVICE_DESCRIPTION lpInfo);

        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "ChangeServiceConfig2W", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeServiceConfig2(ServiceHandle hService, DW_INFO_LEVEL dwInfoLevel, [MarshalAs(UnmanagedType.Struct)] ref SERVICE_CONFIG_DELAYED_AUTO_START_INFO lpInfo);

        [DllImport(DLLFiles.ADVAPI32, EntryPoint = "ChangeServiceConfigW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeServiceConfig(ServiceHandle hService, SERVICE_TYPE dwServiceType, SERVICE_START dwStartType, SERVICE_ERROR_CONTROLE dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ControlService(ServiceHandle hService, SERVICE_CONTROL dwControl, ref SERVICE_STATUS lpServiceStatus);

        //---------------------//

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_STATUS_PROCESS
        {
            public int serviceType;
            public SERVICE_STATE currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            public int processID;
            public int serviceFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_DESCRIPTION
        {
            public string lpDescription;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_CONFIG_DELAYED_AUTO_START_INFO
        {
            public bool delayedStart;
        }

        //---------------------//


        [Flags]
        public enum SCM_ACCESS : uint
        {
            /// <summary>
            /// Required to connect to the service control manager.
            /// </summary>
            SC_MANAGER_CONNECT = 0x00001,

            /// <summary>
            /// Required to call the CreateService function to create a service
            /// object and add it to the database.
            /// </summary>
            SC_MANAGER_CREATE_SERVICE = 0x00002,

            /// <summary>
            /// Required to call the EnumServicesStatusEx function to list the 
            /// services that are in the database.
            /// </summary>
            SC_MANAGER_ENUMERATE_SERVICE = 0x00004,

            /// <summary>
            /// Required to call the LockServiceDatabase function to acquire a 
            /// lock on the database.
            /// </summary>
            SC_MANAGER_LOCK = 0x00008,

            /// <summary>
            /// Required to call the QueryServiceLockStatus function to retrieve 
            /// the lock status information for the database.
            /// </summary>
            SC_MANAGER_QUERY_LOCK_STATUS = 0x00010,

            /// <summary>
            /// Required to call the NotifyBootConfigStatus function.
            /// </summary>
            SC_MANAGER_MODIFY_BOOT_CONFIG = 0x00020,

            /// <summary>
            /// Includes STANDARD_RIGHTS_REQUIRED, in addition to all access 
            /// rights in this table.
            /// </summary>
            SC_MANAGER_ALL_ACCESS = ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
                SC_MANAGER_CONNECT |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_LOCK |
                SC_MANAGER_QUERY_LOCK_STATUS |
                SC_MANAGER_MODIFY_BOOT_CONFIG,

            GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
                SC_MANAGER_ENUMERATE_SERVICE |
                SC_MANAGER_QUERY_LOCK_STATUS,

            GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
                SC_MANAGER_CREATE_SERVICE |
                SC_MANAGER_MODIFY_BOOT_CONFIG,

            GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
                SC_MANAGER_CONNECT | SC_MANAGER_LOCK,

            GENERIC_ALL = SC_MANAGER_ALL_ACCESS,
        }

        /// <summary>
        /// Access to the service. Before granting the requested access, the
        /// system checks the access token of the calling process. 
        /// </summary>
        [Flags]
        public enum SERVICE_ACCESS : uint
        {
            /// <summary>
            /// Required to call the QueryServiceConfig and 
            /// QueryServiceConfig2 functions to query the service configuration.
            /// </summary>
            SERVICE_QUERY_CONFIG = 0x00001,

            /// <summary>
            /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function 
            /// to change the service configuration. Because this grants the caller 
            /// the right to change the executable file that the system runs, 
            /// it should be granted only to administrators.
            /// </summary>
            SERVICE_CHANGE_CONFIG = 0x00002,

            /// <summary>
            /// Required to call the QueryServiceStatusEx function to ask the service 
            /// control manager about the status of the service.
            /// </summary>
            SERVICE_QUERY_STATUS = 0x00004,

            /// <summary>
            /// Required to call the EnumDependentServices function to enumerate all 
            /// the services dependent on the service.
            /// </summary>
            SERVICE_ENUMERATE_DEPENDENTS = 0x00008,

            /// <summary>
            /// Required to call the StartService function to start the service.
            /// </summary>
            SERVICE_START = 0x00010,

            /// <summary>
            ///     Required to call the ControlService function to stop the service.
            /// </summary>
            SERVICE_STOP = 0x00020,

            /// <summary>
            /// Required to call the ControlService function to pause or continue 
            /// the service.
            /// </summary>
            SERVICE_PAUSE_CONTINUE = 0x00040,

            /// <summary>
            /// Required to call the EnumDependentServices function to enumerate all
            /// the services dependent on the service.
            /// </summary>
            SERVICE_INTERROGATE = 0x00080,

            /// <summary>
            /// Required to call the ControlService function to specify a user-defined
            /// control code.
            /// </summary>
            SERVICE_USER_DEFINED_CONTROL = 0x00100,

            /// <summary>
            /// Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.
            /// </summary>
            SERVICE_ALL_ACCESS = (ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
                SERVICE_QUERY_CONFIG |
                SERVICE_CHANGE_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_ENUMERATE_DEPENDENTS |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_INTERROGATE |
                SERVICE_USER_DEFINED_CONTROL),

            GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
                SERVICE_QUERY_CONFIG |
                SERVICE_QUERY_STATUS |
                SERVICE_INTERROGATE |
                SERVICE_ENUMERATE_DEPENDENTS,

            GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
                SERVICE_CHANGE_CONFIG,

            GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
                SERVICE_START |
                SERVICE_STOP |
                SERVICE_PAUSE_CONTINUE |
                SERVICE_USER_DEFINED_CONTROL,

            /// <summary>
            /// Required to call the QueryServiceObjectSecurity or 
            /// SetServiceObjectSecurity function to access the SACL. The proper
            /// way to obtain this access is to enable the SE_SECURITY_NAME 
            /// privilege in the caller's current access token, open the handle 
            /// for ACCESS_SYSTEM_SECURITY access, and then disable the privilege.
            /// </summary>
            ACCESS_SYSTEM_SECURITY = ACCESS_MASK.ACCESS_SYSTEM_SECURITY,

            /// <summary>
            /// Required to call the DeleteService function to delete the service.
            /// </summary>
            DELETE = ACCESS_MASK.DELETE,

            /// <summary>
            /// Required to call the QueryServiceObjectSecurity function to query
            /// the security descriptor of the service object.
            /// </summary>
            READ_CONTROL = ACCESS_MASK.READ_CONTROL,

            /// <summary>
            /// Required to call the SetServiceObjectSecurity function to modify
            /// the Dacl member of the service object's security descriptor.
            /// </summary>
            WRITE_DAC = ACCESS_MASK.WRITE_DAC,

            /// <summary>
            /// Required to call the SetServiceObjectSecurity function to modify 
            /// the Owner and Group members of the service object's security 
            /// descriptor.
            /// </summary>
            WRITE_OWNER = ACCESS_MASK.WRITE_OWNER,
        }

        /// <summary>
        /// Service types.
        /// </summary>
        [Flags]
        public enum SERVICE_TYPE : uint
        {
            SERVICE_NO_CHANGE = 0xffffffff,

            /// <summary>
            /// Driver service.
            /// </summary>
            SERVICE_KERNEL_DRIVER = 0x00000001,

            /// <summary>
            /// File system driver service.
            /// </summary>
            SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,

            /// <summary>
            /// Service that runs in its own process.
            /// </summary>
            SERVICE_WIN32_OWN_PROCESS = 0x00000010,

            /// <summary>
            /// Service that shares a process with one or more other services.
            /// </summary>
            SERVICE_WIN32_SHARE_PROCESS = 0x00000020,

            /// <summary>
            /// The service can interact with the desktop.
            /// </summary>
            SERVICE_INTERACTIVE_PROCESS = 0x00000100,
        }

        /// <summary>
        /// Service start options
        /// </summary>
        public enum SERVICE_START : uint
        {
            /// <summary>
            /// A device driver started by the system loader. This value is valid
            /// only for driver services.
            /// </summary>
            SERVICE_BOOT_START = 0x00000000,

            /// <summary>
            /// A device driver started by the IoInitSystem function. This value 
            /// is valid only for driver services.
            /// </summary>
            SERVICE_SYSTEM_START = 0x00000001,

            /// <summary>
            /// A service started automatically by the service control manager 
            /// during system startup. For more information, see Automatically 
            /// Starting Services.
            /// </summary>         
            SERVICE_AUTO_START = 0x00000002,

            /// <summary>
            /// A service started by the service control manager when a process 
            /// calls the StartService function. For more information, see 
            /// Starting Services on Demand.
            /// </summary>
            SERVICE_DEMAND_START = 0x00000003,

            /// <summary>
            /// A service that cannot be started. Attempts to start the service
            /// result in the error code ERROR_SERVICE_DISABLED.
            /// </summary>
            SERVICE_DISABLED = 0x00000004,
        }

        [Flags]
        public enum SERVICE_ERROR_CONTROLE : uint
        {
            SERVICE_NO_CHANGE = 0xffffffff,

            SERVICE_ERROR_CRITICAL = 0x00000003,
            SERVICE_ERROR_IGNORE = 0x00000000,
            SERVICE_ERROR_NORMAL = 0x00000001,
            SERVICE_ERROR_SEVERE = 0x00000002
        }

        [Flags]
        public enum SERVICE_STATE : uint
        {
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_STOPPED = 0x00000001
        }

        [Flags]
        public enum SERVICE_CONTROL : uint
        {
            STOP = 0x00000001,
            PAUSE = 0x00000002,
            CONTINUE = 0x00000003,
            INTERROGATE = 0x00000004,
            SHUTDOWN = 0x00000005,
            PARAMCHANGE = 0x00000006,
            NETBINDADD = 0x00000007,
            NETBINDREMOVE = 0x00000008,
            NETBINDENABLE = 0x00000009,
            NETBINDDISABLE = 0x0000000A,
            DEVICEEVENT = 0x0000000B,
            HARDWAREPROFILECHANGE = 0x0000000C,
            POWEREVENT = 0x0000000D,
            SESSIONCHANGE = 0x0000000E
        }

        [Flags]
        public enum SERVICE_ACCEPT : uint
        {
            STOP = 0x00000001,
            PAUSE_CONTINUE = 0x00000002,
            SHUTDOWN = 0x00000004,
            PARAMCHANGE = 0x00000008,
            NETBINDCHANGE = 0x00000010,
            HARDWAREPROFILECHANGE = 0x00000020,
            POWEREVENT = 0x00000040,
            SESSIONCHANGE = 0x00000080,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SERVICE_STATUS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        [Flags]
        public enum ACCESS_MASK : uint
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,

            STANDARD_RIGHTS_REQUIRED = 0x000F0000,

            STANDARD_RIGHTS_READ = 0x00020000,
            STANDARD_RIGHTS_WRITE = 0x00020000,
            STANDARD_RIGHTS_EXECUTE = 0x00020000,

            STANDARD_RIGHTS_ALL = 0x001F0000,

            SPECIFIC_RIGHTS_ALL = 0x0000FFFF,

            ACCESS_SYSTEM_SECURITY = 0x01000000,

            MAXIMUM_ALLOWED = 0x02000000,

            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000,

            DESKTOP_READOBJECTS = 0x00000001,
            DESKTOP_CREATEWINDOW = 0x00000002,
            DESKTOP_CREATEMENU = 0x00000004,
            DESKTOP_HOOKCONTROL = 0x00000008,
            DESKTOP_JOURNALRECORD = 0x00000010,
            DESKTOP_JOURNALPLAYBACK = 0x00000020,
            DESKTOP_ENUMERATE = 0x00000040,
            DESKTOP_WRITEOBJECTS = 0x00000080,
            DESKTOP_SWITCHDESKTOP = 0x00000100,

            WINSTA_ENUMDESKTOPS = 0x00000001,
            WINSTA_READATTRIBUTES = 0x00000002,
            WINSTA_ACCESSCLIPBOARD = 0x00000004,
            WINSTA_CREATEDESKTOP = 0x00000008,
            WINSTA_WRITEATTRIBUTES = 0x00000010,
            WINSTA_ACCESSGLOBALATOMS = 0x00000020,
            WINSTA_EXITWINDOWS = 0x00000040,
            WINSTA_ENUMERATE = 0x00000100,
            WINSTA_READSCREEN = 0x00000200,

            WINSTA_ALL_ACCESS = 0x0000037F
        }

        [Flags]
        public enum DW_INFO_LEVEL : uint
        {
            SERVICE_CONFIG_DESCRIPTION = 0x00000001,
            SERVICE_CONFIG_FAILURE_ACTIONS = 0x00000002,
            SERVICE_CONFIG_DELAYED_AUTO_START_INFO = 0x00000003,
            SERVICE_CONFIG_FAILURE_ACTIONS_FLAG = 0x00000004,
            SERVICE_CONFIG_SERVICE_SID_INFO = 0x00000005,
            SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO = 0x00000006,
            SERVICE_CONFIG_PRESHUTDOWN_INFO = 0x00000007
        }
    }
}
