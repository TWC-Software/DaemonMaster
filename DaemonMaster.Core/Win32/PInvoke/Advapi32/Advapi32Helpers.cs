using System;
using System.Diagnostics;
using System.Text;

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        /// <summary>
        /// Join service dependencies and group dependencies string arrays and add double null termination to them
        /// </summary>
        /// <param name="stringServiceDependencies">String array of services on which the service is dependent</param>
        /// <param name="stringGroupDependencies">String array of groups on which the service is dependent</param>
        /// <returns>The converted string</returns>
        public static string ConvertDependenciesArraysToWin32String(string[] stringServiceDependencies, string[] stringGroupDependencies)
        {
            var stringBuilder = new StringBuilder();

            foreach (string item in stringServiceDependencies)
            {
                stringBuilder.Append(item.Trim()).Append("\0");
            }

            foreach (string item in stringGroupDependencies)
            {
                stringBuilder.Append(ScGroupIdentifier + item.Trim()).Append("\0");
            }

            //Double null termination
            stringBuilder.Append("\0");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts a string to an LSA_UNICODE_STRING.
        /// </summary>
        /// <param name="s">The string that should be converted.</param>
        /// <returns>The converted string</returns>
        /// <exception cref="ArgumentException">String too long to create a LSA_UNICODE_STRING. - s</exception>
        public static LsaUnicodeString ToLsaString(this string s)
        {
            // Unicode strings max. 32KB
            if (s.Length > 0x7FFE)
                throw new ArgumentException("String too long to create a LSA_UNICODE_STRING.", nameof(s));

            LsaUnicodeString lus = new LsaUnicodeString();
            lus.Buffer = s;
            lus.Length = (ushort)(s.Length * UnicodeEncoding.CharSize);
            lus.MaximumLength = (ushort)(lus.Length + UnicodeEncoding.CharSize);

            return lus;
        }
    }
}
