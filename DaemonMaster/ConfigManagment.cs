using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DaemonMaster
{
    public static class ConfigManagment
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //                                            REGISTRY                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Registry

        public static void SaveConfigInRegistry(Config config)
        {
            using (RegistryKey serviceKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\DaemonMaster"))
            {
                serviceKey.SetValue("Language", config.Language, RegistryValueKind.String);
            }
        }

        public static Config LoadConfigFromRegistry()
        {

            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\DaemonMaster", false))
            {
                if (key == null)
                    throw new Exception("Can't open registry key!");

                Config config = new Config();

                config.Language = (string)key.GetValue("Language");

                return config;
            }
        }

        public static void DeleteConfigFromRegistry()
        {
            //Open Regkey folder
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\", true))
            {
                if (key == null)
                    throw new Exception("Can't delete config registry keys!");

                key.DeleteSubKeyTree("DaemonMaster");
            }
        }

        #endregion
    }
}
