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

using Newtonsoft.Json;
using System;
using System.IO;

namespace DaemonMasterCore.Config
{
    public static class ConfigManagement
    {
        private static readonly string ConfigFile = AppDomain.CurrentDomain.BaseDirectory + "settings.config";
        public static Config Config = null;

        public static void LoadConfig()
        {
            if (!File.Exists(ConfigFile))
                SaveConfig();

            using (StreamReader streamReader = File.OpenText(ConfigFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                Config = (Config)serializer.Deserialize(streamReader, typeof(Config));
            }
        }

        public static void SaveConfig()
        {
            using (StreamWriter streamWriter = File.CreateText(ConfigFile))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(streamWriter, Config);
            }
        }
    }
}
