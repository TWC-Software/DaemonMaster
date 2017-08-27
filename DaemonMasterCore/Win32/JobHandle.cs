using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DaemonMasterCore.Win32
{
    public class JobHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public JobHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }

        public static JobHandle CreateJob(NativeMethods.SECURITY_ATTRIBUTES lpJobAttributes, string lpName)
        {
            JobHandle jobHandle = NativeMethods.CreateJobObject(lpJobAttributes, lpName);

            if (jobHandle.IsInvalid)
                throw new Win32Exception("Unable to create job. Error: " + Marshal.GetLastWin32Error());

            return jobHandle;
        }

        public void SetInformation(NativeMethods.JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength)
        {
            if (!NativeMethods.SetInformationJobObject(this, infoType, lpJobObjectInfo, cbJobObjectInfoLength))
                throw new Win32Exception("Unable to set job informations. Error: " + Marshal.GetLastWin32Error());
        }

        public void AssignProcess(SafeProcessHandle hProcess)
        {
            if (!NativeMethods.AssignProcessToJobObject(this, hProcess))
                throw new Win32Exception("Unable to assign process to the job. Error: " + Marshal.GetLastWin32Error());
        }
    }
}
