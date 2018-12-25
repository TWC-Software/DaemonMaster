using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using DaemonMasterCore.Win32.PInvoke.Core;

namespace DaemonMasterCore.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public const uint ServiceNoChange = 0xFFFFFFFF;
        public const uint ScStatusProcessInfo = 0;

        private const string DllName = "advapi32.dll";

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ServiceHandle CreateService
        (
            ServiceControlManager serviceControlManager,
            string serviceName,
            string displayName,
            ServiceAccessRights desiredAccess,
            ServiceType serviceType,
            ServiceStartType startType,
            ErrorControl errorControl,
            string binaryPathName,
            string loadOrderGroup,
            uint tagId,
            StringBuilder dependencies,
            string serviceUsername,
            IntPtr serviceassword
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ServiceControlManager OpenSCManager
        (
            string machineName,
            string databaseName,
            ServiceControlManagerAccessRights desiredAccess
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ServiceHandle OpenService
        (
            ServiceControlManager serviceControlManager,
            string serviceName,
            ServiceAccessRights desiredAccess
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle
        (
            IntPtr serviceControlManager
        );


        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService
        (
            ServiceHandle serviceHandle,
            uint numberOfArgs,
            string[] args
        );

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ControlService
        (
            ServiceHandle serviceHandle,
            ServiceControl control,
            ref ServiceStatus lpServiceStatus
        );

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteService
        (
            ServiceHandle serviceHandle
        );


        [DllImport(DllName, SetLastError = true)]
        public static extern bool QueryServiceStatusEx
        (
            ServiceHandle serviceHandle,
            uint infoLevel,
            IntPtr buffer,
            uint bufferSize,
            out uint bytesNeeded
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig
        (
            ServiceHandle serviceHandle,
            ServiceType serviceType,
            ServiceStartType startType,
            ErrorControl errorControl,
            string binaryPathName,
            string loadOrderGroup,
            uint tagId,
            StringBuilder dependencies,
            string serviceUsername,
            IntPtr serviceassword,
            string displayName
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig2
        (
            ServiceHandle serviceHandle,
            ServiceInfoLevel infoLevel,
            IntPtr info
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LogonUser
        (
            string username,
            string domain,
            IntPtr password,
            LogonType dwLogonType,
            LogonProvider dwLogonProvider,
            out TokenHandle logonToken
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser
        (
            TokenHandle logonToken,
            string applicationName,
            StringBuilder commandLineArgs,
            Kernel32.Kernel32.SecurityAttributes processAttributes,
            Kernel32.Kernel32.SecurityAttributes threadAttributes,
            bool inheritHandles,
            CreationFlags creationFlags,
            IntPtr lpEnvironment,
            string currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation
       );

        //LSA 

        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaOpenPolicy
        (
            LsaUnicodeString systemName,
            ref LsaObjectAttributes objectAttributes,
            Kernel32.Kernel32.AccessMask.PolicySpecificRights accessMask,
            out LsaPolicyHandle policyHandle
        );

        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaAddAccountRights
        (
            LsaPolicyHandle policyHandle,
            IntPtr accountSid,
            LsaUnicodeString[] userRights,
            uint countOfRights
        );

        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaRemoveAccountRights
        (
            LsaPolicyHandle policyHandle,
            IntPtr accountSid,
            bool allRights,
            LsaUnicodeString[] userRights,
            uint countOfRights
        );

        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaEnumerateAccountRights
        (
            LsaPolicyHandle policyHandle,
            IntPtr accountSid,
            out IntPtr userRights,
            out uint countOfRights
        );

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaClose(IntPtr policyHandle);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaFreeMemory(IntPtr buffer);

        [DllImport(DllName, ExactSpelling = true)]
        public static extern int LsaNtStatusToWinError(NtStatus ntStatus);
    }
}
