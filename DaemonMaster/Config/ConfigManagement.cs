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

namespace DaemonMaster.Config
{
    public static class ConfigManagement
    {
        private static readonly string ConfigFile = AppDomain.CurrentDomain.BaseDirectory + "settings.config";

        public static DaemonMaster.Config.Config GetConfig { get; private set; } = new DaemonMaster.Config.Config();


        public static DaemonMaster.Config.Config LoadConfig()
        {
            if (!File.Exists(ConfigFile))
                SaveConfig();

            using (StreamReader streamReader = File.OpenText(ConfigFile))
            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer()
                {
                    TypeNameHandling = TypeNameHandling.None,
                };

                GetConfig = serializer.Deserialize<DaemonMaster.Config.Config>(jsonTextReader);
                return GetConfig;
            }
        }

        public static void SaveConfig()
        {
            using (StreamWriter streamWriter = File.CreateText(ConfigFile))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = new JsonSerializer
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.None,
                };

                serializer.Serialize(jsonWriter, GetConfig);
                jsonWriter.Flush();
            }
        }
    }
}
