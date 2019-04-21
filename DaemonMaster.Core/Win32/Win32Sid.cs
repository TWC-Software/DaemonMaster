using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using DaemonMaster.Core.Win32.PInvoke.Advapi32;

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
                throw new ArgumentException("Failed to instantiate Win32Sid: String is empty or null!");


            //var sid = (SecurityIdentifier)new NTAccount(DaemonMasterUtils.GetLoginFromUsername(account)).Translate(typeof(SecurityIdentifier));

            //var buffer = new byte[sid.BinaryLength];
            //sid.GetBinaryForm(buffer, 0);

            //Pointer = Marshal.AllocHGlobal(buffer.Length);
            //Marshal.Copy(buffer, 0, Pointer, buffer.Length);

            Pointer = IntPtr.Zero;
            uint cbSid = 0;
            var referencedDomainName = new StringBuilder();
            var cbReferencedDomainName = (uint)referencedDomainName.Capacity;

            if (!Advapi32.LookupAccountName(null, DaemonMasterUtils.GetLoginFromUsername(account), Pointer, ref cbSid, referencedDomainName, ref cbReferencedDomainName, out uint sidUse))
            {
                int result = Marshal.GetLastWin32Error();

                if (result == 0x7A) //ERROR_INSUFFICIENT_BUFFER
                {
                    Pointer = Marshal.AllocHGlobal((int)cbSid);
                    referencedDomainName.EnsureCapacity((int)cbReferencedDomainName);
                    referencedDomainName.Append(DaemonMasterUtils.GetDomainFromUsername(account));

                    if (!Advapi32.LookupAccountName(null, DaemonMasterUtils.GetLoginFromUsername(account), Pointer, ref cbSid, referencedDomainName, ref cbReferencedDomainName, out sidUse))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    throw new Win32Exception(result);
                }
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
            {
                Advapi32.FreeSid(Pointer);
            }
        }

        ~Win32Sid()
        {
            ReleaseUnmanagedResources();
        }
    }
}
