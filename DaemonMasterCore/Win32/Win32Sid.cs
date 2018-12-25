using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace DaemonMasterCore.Win32
{
    /// <inheritdoc />
    /// <summary>
    /// Class that read the Win32Sid infos and give them as pointer
    /// </summary>
    internal sealed class Win32Sid : IDisposable
    {
        public IntPtr Pointer { get; }

        public Win32Sid(string account)
        {
            if (string.IsNullOrWhiteSpace(account))
                throw new ArgumentException("Failed to instantiate Win32Sid: String is empty or null!");


            var sid = (SecurityIdentifier)new NTAccount(DaemonMasterUtils.GetDomainFromUsername(account), DaemonMasterUtils.GetLoginFromUsername(account)).Translate(typeof(SecurityIdentifier));

            var buffer = new byte[sid.BinaryLength];
            sid.GetBinaryForm(buffer, 0);

            Pointer = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, Pointer, buffer.Length);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            if (Pointer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(Pointer);
            }
        }

        ~Win32Sid()
        {
            ReleaseUnmanagedResources();
        }
    }
}
