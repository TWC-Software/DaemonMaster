/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: PROCESS MANAGEMENT CONFIG FILE
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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaemonMasterCore.Win32;

namespace DaemonMasterCore
{
    public static class ProcessManagement
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

            if (processHandle == IntPtr.Zero)
                return false;

            try
            {
                return KERNEL32.SuspendThread(processHandle);
            }
            finally
            {
                KERNEL32.CloseHandle(processHandle);
            }
        }

        public static bool ResumeProcess(uint Id)
        {
            IntPtr processHandle = KERNEL32.OpenThread(KERNEL32.ThreadAccess.SUSPEND_RESUME, true, (uint)Id);

            if (processHandle == IntPtr.Zero)
                return false;

            try
            {
                return KERNEL32.ResumeThread(processHandle);
            }
            finally
            {
                KERNEL32.CloseHandle(processHandle);
            }
        }
    }
}
