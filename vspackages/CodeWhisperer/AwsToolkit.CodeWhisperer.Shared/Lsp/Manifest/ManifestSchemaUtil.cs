using System.IO;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest.Models;

using Newtonsoft.Json;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Manifest
{
    public class ManifestSchemaUtil
    {
        /// <summary>
        /// Load from file
        /// </summary>
        public static async Task<ManifestSchema> LoadAsync(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return await LoadAsync(stream);
            }
        }

        /// <summary>
        /// Load from stream
        /// </summary>
        public static async Task<ManifestSchema> LoadAsync(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync();
                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    // gracefully handle any missing/new members found in json string
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };
                var manifest = JsonConvert.DeserializeObject<ManifestSchema>(json, jsonSerializerSettings);
                return manifest;
            }
        }
    }
}
