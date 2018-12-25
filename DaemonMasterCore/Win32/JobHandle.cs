/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: JobHandle
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
using System.ComponentModel;
using System.Runtime.InteropServices;
using DaemonMasterCore.Win32.PInvoke.Kernel32;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Win32
{
    public class JobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public JobHandle() : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Kernel32.CloseHandle(handle);
        }

        public static JobHandle CreateJob(Kernel32.SecurityAttributes jobAttributes, string name)
        {
            JobHandle jobHandle = Kernel32.CreateJobObject(jobAttributes, name);

            if (jobHandle.IsInvalid)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            return jobHandle;
        }

        public void SetInformation(Kernel32.JobObjectInfoType infoType, IntPtr jobObjectInfo, uint jobObjectInfoLength)
        {
            if (!Kernel32.SetInformationJobObject(this, infoType, jobObjectInfo, jobObjectInfoLength))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void AssignProcess(SafeProcessHandle processHandlerocess)
        {
            if (!Kernel32.AssignProcessToJobObject(this, processHandlerocess))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
