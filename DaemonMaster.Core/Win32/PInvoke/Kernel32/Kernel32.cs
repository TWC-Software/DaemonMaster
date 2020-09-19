using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DaemonMaster.Core.Win32.PInvoke.Kernel32
{
    public static partial class Kernel32
    {
        public const uint Infinite = 0xFFFFFFFF;
        public const uint NoActiveConsoleSession = 0xFFFFFFFF;
        public const uint CstrLessThan = 1;     // string 1 < string 2
        public const uint CstrEqual = 2;        // string 1 == string 2
        public const uint CstrGreaterThan = 3;  // string 1 > string 2

        private const string DllName = "kernel32.dll";

        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(CtrlEvent ctrlEvent, uint processGroupId);

        [DllImport(DllName, ExactSpelling = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern JobHandle CreateJobObject(SecurityAttributes jobAttributes, string jobName);

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(JobHandle jobHandle, JobObjectInfoType infoType, IntPtr jobObjectInfo, uint jobObjectInfoLength);

        [DllImport(DllName, SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(JobHandle jobHandle, SafeProcessHandle processHandle);

        [DllImport(DllName, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int CompareStringOrdinal(IntPtr string1, int charCount1, IntPtr string2, int charCount2, bool ignoreCase);

        [DllImport(DllName, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcess
        (
            string applicationName,
            string commandLineArgs,
            SecurityAttributes processAttributes,
            SecurityAttributes threadAttributes,
            bool inheritHandles,
            CreationFlags creationFlags,
            IntPtr lpEnvironment,
            string currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation
        );

        [DllImport(DllName, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(
            SafeProcessHandle processHandle,
            int exitCode
        );
    }
}
