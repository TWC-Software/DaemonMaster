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

        public static LsaUnicodeString ToLsaString(this string s)
        {
            // Unicode strings max. 32KB
            if (s.Length > 0x7ffe)
                throw new ArgumentException("String to long for converting into a LSA_UNICODE_STRING.");

            var lus = new LsaUnicodeString
            {
                //Buffer = s, //TODO
                Length = (ushort)(s.Length * UnicodeEncoding.CharSize),
                MaximumLength = (ushort)((s.Length + 1) * UnicodeEncoding.CharSize)
            };

            return lus;
        }
    }
}
