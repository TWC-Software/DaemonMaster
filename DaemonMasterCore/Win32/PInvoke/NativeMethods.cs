/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: NativeMethods
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
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32.PInvoke
{
    public static partial class NativeMethods
    {
        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ServiceHandle CreateService
        (
            ServiceControlManager hSCManager,
            string lpServiceName,
            string lpDisplayName,
            SERVICE_ACCESS dwDesiredAccess,
            uint dwServiceType,
            SERVICE_START dwStartType,
            SERVICE_ERROR_CONTROL dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            string lpdwTagId,
            StringBuilder lpDependencies,
            string lpServiceStartName,
            IntPtr lpPassword
        );


        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ServiceControlManager OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwAccess);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern ServiceHandle OpenService(ServiceControlManager hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCManager);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(ServiceHandle hService, uint dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ControlService(ServiceHandle hService, SERVICE_CONTROL dwControl, ref SERVICE_STATUS lpServiceStatus);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteService(ServiceHandle hService);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryServiceStatusEx(ServiceHandle hService, uint infoLevel, IntPtr buffer, int bufferSize, out int bytesNeeded);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig(ServiceHandle hService, uint dwServiceType, SERVICE_START dwStartType, SERVICE_ERROR_CONTROL dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId, StringBuilder lpDependencies, string lpServiceStartName, IntPtr lpPassword, string lpDisplayName);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig2(ServiceHandle hService, INFO_LEVEL dwInfoLevel, [MarshalAs(UnmanagedType.Struct)] ref SERVICE_DESCRIPTION lpInfo);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig2(ServiceHandle hService, INFO_LEVEL dwInfoLevel, [MarshalAs(UnmanagedType.Struct)] ref SERVICE_CONFIG_DELAYED_AUTO_START_INFO lpInfo);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool LogonUser(
            string lpszUsername,
            string lpszDomain,
            IntPtr lpszPassword,
            LOGON_TYP dwLogonType,
            LOGON_PROVIDER dwLogonProvider,
            out TokenHandle phToken
        );

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser(
            TokenHandle hToken,
            string lpApplicationName,
            StringBuilder lpCommandLine,
            SECURITY_ATTRIBUTES lpProcessAttributes,
            SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr? lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint LsaOpenPolicy(
            LSA_UNICODE_STRING[] SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            int AccessMask,
            ref IntPtr PolicyHandle
        );

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint LsaAddAccountRights(
            LsaPolicyHandle PolicyHandle,
            IntPtr AccountSid,
            LSA_UNICODE_STRING[] UserRights,
            uint CountOfRights
        );

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint LsaRemoveAccountRights(
            LsaPolicyHandle PolicyHandle,
            IntPtr AccountSid,
            bool AllRights,
            LSA_UNICODE_STRING[] UserRights,
            uint CountOfRights
        );

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint LsaEnumerateAccountRights(
            LsaPolicyHandle PolicyHandle,
            IntPtr AccountSid,
            ref IntPtr UserRights,
            out uint CountOfRights
        );

        [DllImport(DLLFiles.ADVAPI32)]
        public static extern int LsaNtStatusToWinError(uint NTSTATUS);

        [DllImport(DLLFiles.ADVAPI32)]
        public static extern uint LsaClose(IntPtr PolicyHandle);

        [DllImport(DLLFiles.ADVAPI32)]
        public static extern uint LsaFreeMemory(IntPtr Buffer);


        //KERNEL 32

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ThreadHandle OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SuspendThread(ThreadHandle hThread);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ResumeThread(ThreadHandle hThread);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(CtrlEvent dwCtrlEvent, uint dwProcessGroupId);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int WTSGetActiveConsoleSessionId();

        [DllImport(DLLFiles.KERNEL32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern JobHandle CreateJobObject(SECURITY_ATTRIBUTES lpJobAttributes, string lpName);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(JobHandle hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(JobHandle hJob, SafeProcessHandle hProcess);


        //WINSTA

        [DllImport(DLLFiles.WINSTA, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern void WinStationSwitchToServicesSession();


        //WTSAPI32

        [DllImport(DLLFiles.WTSAPI32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(int sessionId, out TokenHandle token);
    }
}
