//  DaemonMaster: ThreadHandle
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
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32
{
    public class ThreadHandle : SafeHandle
    {
        public ThreadHandle() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public static ThreadHandle OpenThread(NativeMethods.ThreadAccess desiredAccess, bool inheritHandle, int processId)
        {
            ThreadHandle threadHandle = NativeMethods.OpenThread(desiredAccess, inheritHandle, (uint)processId);

            if (threadHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return threadHandle;
        }

        public void PauseThread()
        {
            if (!NativeMethods.SuspendThread(this))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void ResumeThread()
        {
            if (!NativeMethods.ResumeThread(this))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
