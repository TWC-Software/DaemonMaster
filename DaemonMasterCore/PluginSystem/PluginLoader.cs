/*
   Copyright 2013 Christoph Gattnar (edited by MCPC10)

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DaemonMasterCore.PluginSystem
{
    public class PluginLoader
    {
#if DEBUG
        public static readonly string DaemonMasterPluginDir = AppDomain.CurrentDomain.BaseDirectory + "Plugins";

        private ICollection<PluginContracts.IPlugin> plugins = new List<PluginContracts.IPlugin>();


        public PluginLoader(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                string[] dllFiles = Directory.GetFiles(path, "*.dll");

                ICollection<Assembly> assemblies = new List<Assembly>(dllFiles.Length);
                foreach (var dllFile in dllFiles)
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(dllFile);
                    Assembly assembly = Assembly.Load(assemblyName);
                    assemblies.Add(assembly);
                }

                Type pluginType = typeof(PluginContracts.IPlugin);
                ICollection<Type> pluginTypes = new List<Type>();
                foreach (var assembly in assemblies)
                {
                    if (assembly == null)
                        continue;

                    Type[] assemblyTypes = assembly.GetTypes();
                    foreach (var assemblyType in assemblyTypes)
                    {
                        if (assemblyType.IsInterface || assemblyType.IsAbstract)
                        {
                            continue;
                        }

                        if (assemblyType.GetInterface(pluginType.FullName) == null)
                            continue;

                        pluginTypes.Add(assemblyType);
                    }
                }

                foreach (var type in pluginTypes)
                {
                    PluginContracts.IPlugin plugin = (PluginContracts.IPlugin)Activator.CreateInstance(type);
                    plugins.Add(plugin);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }


        internal void Init(IAPI api)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Init(api);
                }
                catch (Exception e)
                {
                    //TODO: log
                    plugins.Remove(plugin);
                }
            }
        }

        internal void UnLoad()
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    //TODO: Log
                    plugin.UnLoad();
                }
                catch (Exception)
                {
                    //TODO: Log
                }
            }
        }
#endif
    }
}
