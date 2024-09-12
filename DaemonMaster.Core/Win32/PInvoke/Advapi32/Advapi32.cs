using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public const uint ServiceNoChange = 0xFFFFFFFF;
        public const uint ScStatusProcessInfo = 0;
        public const string ScGroupIdentifier = "+";

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
            string dependencies,
            string serviceUsername,
            IntPtr servicePassword
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
            IntPtr args
        );

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ControlService
        (
            ServiceHandle serviceHandle,
            ServiceControl control,
            ref ServiceStatus serviceStatus
        );

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ControlService
        (
            ServiceHandle serviceHandle,
            int control,
            ref ServiceStatus serviceStatus
        );

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteService
        (
            ServiceHandle serviceHandle
        );


        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryServiceStatusEx
        (
            ServiceHandle serviceHandle,
            uint infoLevel,
            IntPtr buffer,
            uint bufferSize,
            ref uint bytesNeeded
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryServiceConfig
        (
            ServiceHandle serviceHandle,
            IntPtr buffer,
            uint bufferSize,
            ref uint bytesNeeded
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryServiceConfig2
        (
            ServiceHandle serviceHandle,
            ServiceInfoLevel infoLevel,
            IntPtr buffer,
            uint bufferSize,
            ref uint bytesNeeded
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
            string dependencies,
            string serviceUsername,
            IntPtr servicePassword,
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
        public static extern bool GetServiceKeyName
        (
            ServiceControlManager serviceControlManager,
            string displayName,
            IntPtr serviceName,
            ref uint bytesNeeded
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
            out SafeAccessTokenHandle logonToken
        );

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser
        (
            SafeAccessTokenHandle logonToken,
            string? applicationName,
            string? commandLineArgs,
            Kernel32.Kernel32.SecurityAttributes? processAttributes,
            Kernel32.Kernel32.SecurityAttributes? threadAttributes,
            bool inheritHandles,
            Kernel32.Kernel32.CreationFlags creationFlags,
            IntPtr lpEnvironment,
            string? currentDirectory,
            ref Kernel32.Kernel32.StartupInfo startupInfo,
            out Kernel32.Kernel32.ProcessInformation processInformation
       );

        //LSA 

        [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LookupAccountName(string systemName, string accountName, IntPtr sid, ref uint sidSize, StringBuilder referencedDomainName, ref uint referencedDomainNameSize, out uint sidType);

        [DllImport(DllName)]
        public static extern void FreeSid(IntPtr pSid);

        [DllImport(DllName)]
        public static extern bool IsValidSid(IntPtr pSid);

        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaOpenPolicy
        (
            ref LsaUnicodeString systemName,
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
            long countOfRights
        );

        [DllImport(DllName, ExactSpelling = true)]
        public static extern NtStatus LsaRemoveAccountRights
        (
            LsaPolicyHandle policyHandle,
            IntPtr accountSid,
            bool allRights,
            LsaUnicodeString[] userRights,
            long countOfRights
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

        //Token

        [DllImport(DllName, ExactSpelling = true)]
        public static extern bool DuplicateTokenEx
        (
            SafeAccessTokenHandle existingTokenHandle,
            uint desiredAccess,
            IntPtr threadAttributes,
            int tokenType,
            int impersonationLevel,
            out SafeAccessTokenHandle duplicateTokenHandle
        );
    }
}
