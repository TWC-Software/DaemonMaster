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
        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern ServiceHandle CreateService
        (
            ServiceControlManager hSCManager,
            string lpServiceName,
            string lpDisplayName,
            SERVICE_ACCESS dwDesiredAccess,
            SERVICE_TYPE dwServiceType,
            SERVICE_START dwStartType,
            SERVICE_ERROR_CONTROL dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            string lpdwTagId,
            StringBuilder lpDependencies,
            string lpServiceStartName,
            IntPtr lpPassword
        );


        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern ServiceControlManager OpenSCManager(string machineName, string databaseName, SCM_ACCESS dwAccess);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern ServiceHandle OpenService(ServiceControlManager hSCManager, string lpServiceName, SERVICE_ACCESS dwDesiredAccess);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCManager);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(ServiceHandle hService, uint dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ControlService(ServiceHandle hService, SERVICE_CONTROL dwControl, ref SERVICE_STATUS lpServiceStatus);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteService(ServiceHandle hService);

        [DllImport(DLLFiles.ADVAPI32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool QueryServiceStatusEx(ServiceHandle hService, uint infoLevel, IntPtr buffer, int bufferSize, out int bytesNeeded);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig(ServiceHandle hService, SERVICE_TYPE dwServiceType, SERVICE_START dwStartType, SERVICE_ERROR_CONTROL dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, string lpdwTagId, StringBuilder lpDependencies, string lpServiceStartName, string lpPassword, string lpDisplayName);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig2(ServiceHandle hService, INFO_LEVEL dwInfoLevel, [MarshalAs(UnmanagedType.Struct)] ref SERVICE_DESCRIPTION lpInfo);

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
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

        [DllImport(DLLFiles.ADVAPI32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessAsUser(
            TokenHandle hToken,
            string lpApplicationName,
            StringBuilder lpCommandLine,
            SECURITY_ATTRIBUTES lpProcessAttributes,
            SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);


        //KERNEL 32

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern ThreadHandle OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SuspendThread(ThreadHandle hThread);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ResumeThread(ThreadHandle hThread);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(CtrlEvent dwCtrlEvent, uint dwProcessGroupId);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport(DLLFiles.KERNEL32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern JobHandle CreateJobObject(SECURITY_ATTRIBUTES lpJobAttributes, string lpName);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetInformationJobObject(JobHandle hJob, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool AssignProcessToJobObject(JobHandle hJob, SafeProcessHandle hProcess);


        //WINSTA

        [DllImport(DLLFiles.WINSTA, SetLastError = true, CharSet = CharSet.Auto)]
        public static extern void WinStationSwitchToServicesSession();


        //WTSAPI32

        [DllImport(DLLFiles.WTSAPI32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WTSQueryUserToken(UInt32 sessionId, out TokenHandle Token);
    }
}
