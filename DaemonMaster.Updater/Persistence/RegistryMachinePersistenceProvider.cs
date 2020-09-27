using Microsoft.Win32;
using System;

namespace DaemonMaster.Updater.Persistence
{
    public class RegistryMachinePersistenceProvider : IPersistenceProvider
    {
        public string RegistryPath { get; }

        private const string SkippedVersionRegKey = "SkippedVersion";

        public RegistryMachinePersistenceProvider(string regPath)
        {
            RegistryPath = regPath;
        }

        /// <inheritdoc />
        public Version GetSkippedVersion()
        {
            try
            {
                using (RegistryKey reg = Registry.LocalMachine.OpenSubKey(RegistryPath, false))
                {
                    object regVal = reg?.GetValue(SkippedVersionRegKey);

                    if (Version.TryParse(Convert.ToString(regVal), out Version version))
                        return version;
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        /// <inheritdoc />
        public void SetSkippedVersion(Version version)
        {
            using (RegistryKey reg = Registry.LocalMachine.CreateSubKey(RegistryPath))
            {
                if (version != null)
                {
                    reg?.SetValue(SkippedVersionRegKey, version.ToString());
                }
                else
                {
                    reg?.DeleteValue(SkippedVersionRegKey, false);
                }
            }
        }
    }
}
