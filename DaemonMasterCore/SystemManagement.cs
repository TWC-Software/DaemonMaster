using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using DaemonMasterCore.Win32.PInvoke;

namespace DaemonMasterCore
{
    public static class SystemManagement
    {
        public static bool ValidateUser(string username, SecureString password)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Machine))
            {
                return pc.ValidateCredentials(username, SecurityManagement.ConvertSecureStringToString(password));
            }
        }

        public static bool CheckUser(string username, SecureString password)
        {
            if (String.IsNullOrWhiteSpace(username) || password == null)
                throw new ArgumentNullException();

            IntPtr tokenHandle = IntPtr.Zero;
            IntPtr passwordPtr = IntPtr.Zero;

            try
            {
                string password1 = SecurityManagement.ConvertSecureStringToString(password);

                passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
                return NativeMethods.LogonUser(username, ".", passwordPtr, NativeMethods.LOGON_TYP.Interactive, NativeMethods.LOGON_PROVIDER.Default, ref tokenHandle);
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);

                if (tokenHandle != IntPtr.Zero)
                    NativeMethods.CloseHandle(tokenHandle);
            }
        }
    }
}
