/////////////////////////////////////////////////////////////////////////////////////////
//  DaemonMaster: ConfigManagement
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
//   along with Foobar.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DaemonMaster
{
    public static class ConfigManagement
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                              JSON                                                    //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            REGISTRY                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Registry (old system)

        //public static void SaveConfigInRegistry(Config config)
        //{
        //    using (RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\DaemonMaster"))
        //    {
        //        serviceKey.SetValue("Language", config.Language, RegistryValueKind.String);
        //    }
        //}

        //public static Config LoadConfigFromRegistry()
        //{

        //    //Open Reg key folder
        //    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DaemonMaster", false))
        //    {
        //        if (key == null)
        //            throw new Exception("Can't open registry key!");

        //        Config config = new Config();
        //        config.Language = (string)key.GetValue("Language");

        //        return config;
        //    }
        //}

        //public static void DeleteConfigFromRegistry()
        //{
        //    //Open Reg key folder
        //    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\", true))
        //    {
        //        if (key == null)
        //            throw new Exception("Can't delete config registry keys!");

        //        key.DeleteSubKeyTree("DaemonMaster");
        //    }
        //}

        #endregion
    }
}
