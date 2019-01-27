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
//   along with DeamonMaster.  If not, see <http://www.gnu.org/licenses/>.
/////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using Newtonsoft.Json;

namespace DaemonMaster.Core.Config
{
    public static class ConfigManagement
    {
        private static readonly string ConfigFile = AppDomain.CurrentDomain.BaseDirectory + "settings.config";
        private static Config _config = new Config();


        public static Config LoadConfig()
        {
            if (!File.Exists(ConfigFile))
                SaveConfig();

            using (StreamReader streamReader = File.OpenText(ConfigFile))
            {
                var serializer = new JsonSerializer();
                _config = (Config)serializer.Deserialize(streamReader, typeof(Config));
                return _config;
            }
        }

        public static Config GetConfig => _config;

        public static void SaveConfig()
        {
            using (StreamWriter streamWriter = File.CreateText(ConfigFile))
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented
                };
                serializer.Serialize(streamWriter, _config);
            }
        }
    }
}
