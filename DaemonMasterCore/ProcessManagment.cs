using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaemonMasterCore.Win32;

namespace DaemonMasterCore
{
    public static class ProcessManagment
    {
        public static bool CloseConsoleApplication(bool useCtrlC, uint Id)
        {
            if (useCtrlC)
            {
                return KERNEL32.GenerateConsoleCtrlEvent((uint)KERNEL32.CtrlEvent.CTRL_C_EVENT, Id);
            }
            else
            {
                return KERNEL32.GenerateConsoleCtrlEvent((uint)KERNEL32.CtrlEvent.CTRL_BREAK_EVENT, Id);
            }
        }

        public static bool PauseProcess(uint Id)
        {
            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    return KERNEL32.SuspendThread(processHandle);
                }
                finally
                {
                    KERNEL32.CloseHandle(processHandle);
                }
            }

            return false;
        }

        public static bool ResumeProcess(uint Id)
        {
            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, (uint)Id);

            if (processHandle != IntPtr.Zero)
            {
                try
                {
                    return KERNEL32.ResumeThread(processHandle);
                }
                finally
                {
                    KERNEL32.CloseHandle(processHandle);
                }
            }

            return false;
        }
    }
}
