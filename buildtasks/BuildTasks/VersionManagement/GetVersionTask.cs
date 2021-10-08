using System.IO;

using Microsoft.Build.Framework;

using Newtonsoft.Json;

namespace BuildTasks.VersionManagement
{
    /// <summary>
    /// Retrieves the value of a given JSON file's "version" field
    /// </summary>
    public class GetVersionTask : BuildTaskBase
    {
        internal class ReleaseConfigManifest
        {
            public string Version { get; set; }
        }

        /// <summary>
        /// JSON file to read the "version" field from
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Value of version field loaded from <see cref="Filename"/>
        /// </summary>
        [Output]
        public string Version { get; set; }

        public override bool Execute()
        {
            var json = File.ReadAllText(Filename);
            var manifest = JsonConvert.DeserializeObject<ReleaseConfigManifest>(json);

            Version = manifest.Version;
            return true;
        }
    }
}
