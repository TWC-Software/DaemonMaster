/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: SecurityManagement
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
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace DaemonMasterCore
{
    public static class SecurityManagement
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            Security                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Security
        private static byte[] array = new byte[16] { 0x7e, 0x37, 0xa6, 0xde, 0xeb, 0xd7, 0x66, 0xf1, 0x89, 0xae, 0xfd, 0xd2, 0x99, 0xc3, 0xcd, 0x81 };

        /// <summary>
        /// Convert the given string to a SecureString
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static SecureString ConvertStringToSecureString(string data)
        {
            SecureString secString = new SecureString();

            if (data.Length > 0)
            {
                foreach (char c in data.ToCharArray())
                {
                    secString.AppendChar(c);
                }
                return secString;
            }
            return null;
        }

        /// <summary>
        /// Convert the given SecureString to a normal string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static String ConvertSecureStringToString(SecureString data)
        {
            if (data == null)
                return String.Empty;

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
        ///  Using DPAPI to encrypt the given SecureString
        /// </summary>
        /// <param name="password"></param>
        /// <param name="entropy"></param>
        /// <returns></returns>
        public static unsafe byte[] EncryptPassword(SecureString password, byte[] entropy)
        {
            //Source: https://stackoverflow.com/questions/18392538/securestring-to-byte-c-sharp, 17.07.2017
            IntPtr passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(password);
            byte[] bValue = null;
            try
            {
                byte* byteArray = (byte*)passwordPtr.ToPointer();

                // Find the end of the string
                byte* pEnd = byteArray;
                char c = '\0';
                do
                {
                    byte b1 = *pEnd++;
                    byte b2 = *pEnd++;
                    c = '\0';
                    c = (char)(b1 << 8);
                    c += (char)b2;
                } while (c != '\0');

                // Length is effectively the difference here (note we're 2 past end) 
                int length = (int)((pEnd - byteArray) - 2);
                bValue = new byte[length];
                for (int i = 0; i < length; ++i)
                {
                    // Work with data in byte array as necessary, via pointers, here
                    bValue[i] = *(byteArray + i);
                }

                return ProtectedData.Protect(bValue, entropy, DataProtectionScope.LocalMachine);
            }
            finally
            {
                // This will completely remove the data from memory
                Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
            }
        }

        /// <summary>
        ///  Using DPAPI to decrypt the given SecureString
        /// </summary>
        /// <param name="encryptedPassword"></param>
        /// <param name="entropy"></param>
        /// <returns></returns>
        public static SecureString DecryptPassword(byte[] encryptedPassword, byte[] entropy)
        {
            if (encryptedPassword == null || entropy == null)
                throw new ArgumentNullException();

            if (encryptedPassword.Length <= 0 || entropy.Length <= 0)
                return null;

            byte[] entropy2 = new byte[entropy.Length];
            for (int i = 0; i < entropy.Length; i++)
            {
                entropy2[i] = (byte)(array[i] ^ entropy[i]);
            }

            byte[] decryptedPassword = ProtectedData.Unprotect(encryptedPassword, entropy2, DataProtectionScope.LocalMachine);

            if (decryptedPassword.Length > 0)
            {

                SecureString secString = new SecureString();
                foreach (char c in Encoding.Unicode.GetChars(decryptedPassword))
                {
                    secString.AppendChar((char)c);
                }

                return secString;
            }
            return null;
        }

        /// <summary>
        /// Create a random entropy
        /// </summary>
        /// <returns></returns>
        public static byte[] CreateRandomEntropy()
        {
            byte[] entropy = new byte[array.Length];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            byte[] entropy2 = new byte[entropy.Length];
            for (int i = 0; i < entropy.Length; i++)
            {
                entropy2[i] = (byte)(entropy[i] ^ array[i]);
            }

            return entropy2;
        }
        #endregion
    }
}
