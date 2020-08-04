using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using DaemonMaster.Core.Win32.PInvoke.Kernel32;

namespace DaemonMaster.Core
{
    public static class Security
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Security                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Security

        /// <summary>
        /// Convert the given string to a SecureString.
        /// </summary>
        /// <param name="s">The string that should be encrypted.</param>
        /// <returns>A secure string with the contend of the normal string or null if the string was invalid or empty.</returns>
        [Obsolete("Use instead ConvertStringToSecureString because this method is much slower than the unsafe method.")]
        public static SecureString ConvertStringToSecureStringSave(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            var secString = new SecureString();
            foreach (char c in s)
            {
                secString.AppendChar(c);
            }

            secString.MakeReadOnly();
            return secString;
        }

        /// <summary>
        /// Convert the given string to a SecureString.
        /// </summary>
        /// <param name="s">The string that should be encrypted.</param>
        /// <returns>A secure string with the contend of the normal string or null if the string was invalid or empty.</returns>
        public static unsafe SecureString ConvertStringToSecureString(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            fixed (char* ptrChar = s)
            {
                var secString = new SecureString(ptrChar, s.Length);
                secString.MakeReadOnly();
                return secString;
            }
        }

        /// <summary>
        /// Convert the given SecureString to a normal string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ConvertSecureStringToString(this SecureString data)
        {
            if (data == null)
                return null;

            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(data);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }

        /// <summary>
        /// Compare two secure strings in unmanaged code
        /// </summary>
        /// <param name="data1">String 1</param>
        /// <param name="data2">String 2</param>
        /// <returns></returns>
        public static bool IsEquals(this SecureString data1, SecureString data2)
        {
            if (data1 == null && data2 == null)
                return true;

            if (data1 == null || data2 == null)
                return false;

            IntPtr ptr1 = IntPtr.Zero;
            IntPtr ptr2 = IntPtr.Zero;

            try
            {
                ptr1 = Marshal.SecureStringToGlobalAllocUnicode(data1);
                ptr2 = Marshal.SecureStringToGlobalAllocUnicode(data2);

                int result = Kernel32.CompareStringOrdinal(ptr1, data1.Length, ptr2, data2.Length, ignoreCase: false);
                if (result == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return result == Kernel32.CstrEqual;
            }
            finally
            {
                if (ptr1 != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr1);

                if (ptr2 != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr2);
            }
        }
        #endregion
    }
}
