/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: KillChildProcessJob
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
            //Return if the windows version is lower or equal to win7
            if (Environment.OSVersion.Version.Major <= 6 && Environment.OSVersion.Version.Minor <= 1)
                return;

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
            if (!_jobHandle.IsInvalid)
                _jobHandle.AssignProcess(processHandle);
        }

        public void AssignProcess(Process process)
        {
            if (!_jobHandle.IsInvalid)
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
