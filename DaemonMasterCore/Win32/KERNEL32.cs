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
        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern ThreadHandle OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SuspendThread(ThreadHandle hThread);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ResumeThread(ThreadHandle hThread);

        [DllImport(DLLFiles.KERNEL32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);


        //FLAGS

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [Flags]
        public enum CtrlEvent : int
        {
            CTRL_C_EVENT = (0x0000),
            CTRL_BREAK_EVENT = (0x0001)
        }
    }
}

