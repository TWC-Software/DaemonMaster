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
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DaemonMaster.Core.Win32;
using DaemonMaster.Core.Win32.PInvoke.Kernel32;

namespace DaemonMaster.Core.Jobs
{
    public class KillChildProcessJob : IDisposable
    {
        private bool _isDisposed;
        private JobHandle _jobHandle;

        public static bool IsSupportedWindowsVersion => (Environment.OSVersion.Version.Major > 6 || (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor > 1));

        public KillChildProcessJob()
        {
            //Return if the windows version is lower or equal to win7
            if (!IsSupportedWindowsVersion)
                throw new NotSupportedException("KillChildProcessJob is not supported in your windows version.");

            //Create default security attributes
            var securityAttributes = new Kernel32.SecurityAttributes();
            securityAttributes.length = (uint)Marshal.SizeOf(securityAttributes);

            //Create a job handle
            _jobHandle = JobHandle.CreateJob(securityAttributes, "KillChildProcessJob" + Process.GetCurrentProcess().Id);

            //Create basic limit infos
            var jobBasicLimitInformation = new Kernel32.JobObjectBasicLimitInformation()
            {
                LimitFlags = Kernel32.JobObjectlimit.KillOnJobClose
            };

            //Create extended limit infos
            var jobExtendedLimitInformation = new Kernel32.JobObjectExtendedLimitInformation()
            {
                BasicLimitInformation = jobBasicLimitInformation
            };

            //Set the information for the job handle
            int length = Marshal.SizeOf(jobExtendedLimitInformation);
            IntPtr jobExtendedLimitInformationHandle = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(jobExtendedLimitInformation, jobExtendedLimitInformationHandle, false);

                _jobHandle.SetInformation(Kernel32.JobObjectInfoType.ExtendedLimitInformation, jobExtendedLimitInformationHandle, (uint)length);
            }
            finally
            {
                Marshal.FreeHGlobal(jobExtendedLimitInformationHandle);
            }
        }

        public void AssignProcess(Process process)
        {
            if (!_jobHandle.IsInvalid)
                _jobHandle.AssignProcess(process.SafeHandle);
        }


        #region Dispose

        ~KillChildProcessJob()
        {
            Dispose(false);
        }

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
                _jobHandle?.Dispose();
                _jobHandle = null;
            }
            //Free unmanaged objects here
            _isDisposed = true;
        }
        #endregion
    }
}
