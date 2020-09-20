using Newtonsoft.Json;
using System;
using System.IO;

namespace DaemonMaster.Updater.Persistence
{
    internal class JsonPersistenceProvider : IPersistenceProvider
    {
        private readonly string filePath = string.Empty;
        private JsonSettings settings = new JsonSettings();


        public JsonPersistenceProvider(string filePath)
        {
            this.filePath = filePath;
            Load(); //cache data
        }

        /// <inheritdoc />
        public Version GetSkippedVersion()
        {
            return settings.SkippedVersion;
        }

        /// <inheritdoc />
        public void SetSkippedVersion(Version version)
        {
            settings.SkippedVersion = version;
            Save();
        }


        private void Load()
        {
            if (!File.Exists(filePath))
                Save();

            using (StreamReader streamReader = File.OpenText(filePath))
            using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
            {
                var serializer = new JsonSerializer()
                {
                    TypeNameHandling = TypeNameHandling.None,
                };

                settings = serializer.Deserialize<JsonSettings>(jsonTextReader);
            }
        }

        private void Save()
        {
            using (StreamWriter streamWriter = File.CreateText(filePath))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = new JsonSerializer
                {
                    TypeNameHandling = TypeNameHandling.None,
                };

                serializer.Serialize(jsonWriter, settings);
                jsonWriter.Flush();
            }
        }

        [Serializable]
        private class JsonSettings
        {
            public Version SkippedVersion;
        }
    }
}
