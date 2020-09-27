using Microsoft.Win32;
using System;

namespace DaemonMaster.Updater.Persistence
{
    public class RegistryUserPersistenceProvider : IPersistenceProvider
    {
        public string RegistryPath { get; }

        private const string SkippedVersionRegKey = "SkippedVersion";

        public RegistryUserPersistenceProvider(string regPath)
        {
            RegistryPath = regPath;
        }

        /// <inheritdoc />
        public Version GetSkippedVersion()
        {
            try
            {
                using (RegistryKey reg = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
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
            using (RegistryKey reg = Registry.CurrentUser.CreateSubKey(RegistryPath))
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
