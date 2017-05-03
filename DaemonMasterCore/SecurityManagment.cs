using System;
using System.Runtime.InteropServices;
using System.Security;

namespace DaemonMasterCore
{
    public static class SecurityManagment
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Security                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Security

        public static SecureString ConvertStringToSecureString(string value)
        {
            SecureString secString = new SecureString();

            if (value.Length > 0)
            {
                foreach (char c in value.ToCharArray())
                {
                    secString.AppendChar(c);
                }
                return secString;
            }
            return null;
        }

        public static String ConvertSecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        #endregion
    }
}
