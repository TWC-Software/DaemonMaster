using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DaemonMasterCore.Win32;
using DaemonMasterCore.Win32.PInvoke;
using Microsoft.Win32.SafeHandles;

namespace DaemonMasterCore.Jobs
{
    public class KillChildProcessJob : IDisposable
    {
        private bool _isDisposed = false;
        private JobHandle _jobHandle;

        public KillChildProcessJob()
        {
            //Create job
            _jobHandle = JobHandle.CreateJob(null, "KillChildProcessJob" + Process.GetCurrentProcess().Id);

            NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION jobBasicLimitInformation = new NativeMethods.JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = NativeMethods.JobObjectLimitFlags.KillOnJobClose
            };

            NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION jobExtendedLimitInformation = new NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                BasicLimitInformation = jobBasicLimitInformation
            };

            int length = Marshal.SizeOf(typeof(NativeMethods.JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr jobExtendedLimitInformationHandle = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(jobExtendedLimitInformation, jobExtendedLimitInformationHandle, true);

                _jobHandle.SetInformation(NativeMethods.JobObjectInfoType.ExtendedLimitInformation, jobExtendedLimitInformationHandle, (uint)length);
            }
            finally
            {
                Marshal.FreeHGlobal(jobExtendedLimitInformationHandle);
            }
        }

        public void AssignProcess(SafeProcessHandle processHandle)
        {
            _jobHandle.AssignProcess(processHandle);
        }

        public void AssignProcess(Process process)
        {
            _jobHandle.AssignProcess(process.SafeHandle);
        }


        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                //Free managed objects here
                _jobHandle.Dispose();
                _jobHandle = null;
            }
            //Free unmanaged objects here
            _isDisposed = true;
        }

        ~KillChildProcessJob()
        {
            Dispose(false);
        }
        #endregion
    }
}
