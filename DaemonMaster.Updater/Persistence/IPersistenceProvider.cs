using System;

namespace DaemonMaster.Updater.Persistence
{
    interface IPersistenceProvider
    {
        /// <summary>
        /// Gets the currently skipped version.
        /// </summary>
        /// <returns>If no version is skipped, it will return null.</returns>
        Version GetSkippedVersion();

        /// <summary>
        /// Sets the version that should be skipped.
        /// </summary>
        /// <param name="version">The version that should be skipped.</param>
        void SetSkippedVersion(Version version);
    }
}
