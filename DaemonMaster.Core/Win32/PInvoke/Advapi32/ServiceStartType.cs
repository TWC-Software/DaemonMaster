/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ServiceStartType
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

namespace DaemonMaster.Core.Win32.PInvoke.Advapi32
{
    public static partial class Advapi32
    {
        public enum ServiceStartType
        {
            //  StartOnBoot = 0x00000000, //Not supported in DM
            //  StartOnSystemStart = 0x00000001, //Not supported in DM
            AutoStart = 0x00000002,
            StartOnDemand = 0x00000003,
            Disabled = 0x00000004
        }
    }
}
