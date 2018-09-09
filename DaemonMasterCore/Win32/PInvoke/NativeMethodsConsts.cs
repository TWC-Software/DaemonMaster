/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: NativeMethods
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

namespace DaemonMasterCore.Win32.PInvoke
{
    public static partial class NativeMethods
    {
        /// <summary>
        /// Needed for QueryServiceStatusEx as infoLevel
        /// </summary>
        public const uint SC_STATUS_PROCESS_INFO = 0x0;
        public const int CREATE_NEW_CONSOLE = 0x00000010;
        public const string SC_GROUP_IDENTIFIER = "+";

        public const int CREATE_NO_WINDOW = 0x08000000;
        public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        //NTSTATUS
        public const uint STATUS_SUCCESS = 0x00000000;
        public const uint STATUS_ACCESS_DENIED = 0xc0000022;
        public const uint STATUS_INSUFFICIENT_RESOURCES = 0xc000009a;
        public const uint STATUS_NO_MEMORY = 0xc0000017;
    }
}
