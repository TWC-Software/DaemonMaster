using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;
using DaemonMaster.Win32.PInvoke;

namespace DaemonMaster.Core.Win32
{
    /// <inheritdoc />
    /// <summary>
    /// Class that read the Win32Sid infos and give them as pointer
    /// </summary>
    internal sealed class Win32Sid : IDisposable
    {
        public IntPtr Pointer { get; }

        public bool IsValid
        {
            get
            {
                if (Pointer == IntPtr.Zero)
                    return false;

                return Advapi32.IsValidSid(Pointer);
            }
        }


        public Win32Sid(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                throw new ArgumentException("Win32Sid: Account name is empty or null.");

            //When the user is from the local domain, just use the username
            account = DaemonMasterUtils.IsLocalDomain(account) ? DaemonMasterUtils.GetLoginFromUsername(account) : account;

            uint sidSize = 0;
            uint referencedDomainNameSize = 0;
            if (!Advapi32.LookupAccountName(null, account, IntPtr.Zero, ref sidSize, null, ref referencedDomainNameSize, out uint sidType))
            {
                int result = Marshal.GetLastWin32Error();

                if (result != Win32ErrorCodes.ERROR_INSUFFICIENT_BUFFER)
                    throw new Win32Exception(result);
            }

            StringBuilder referencedDomainName = new StringBuilder((int)referencedDomainNameSize);
            Pointer = Marshal.AllocHGlobal((int)sidSize);
            try
            {
                if (!Advapi32.LookupAccountName(null, account, Pointer, ref sidSize, referencedDomainName, ref referencedDomainNameSize, out sidType))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            if (Pointer != IntPtr.Zero)
                Marshal.FreeHGlobal(Pointer);
        }

        ~Win32Sid()
        {
            ReleaseUnmanagedResources();
        }
    }
}
