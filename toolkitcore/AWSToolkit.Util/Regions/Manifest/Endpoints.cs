using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Amazon.AWSToolkit.Regions.Manifest
{
    /// <summary>
    /// Structure to deserialize what the Toolkit uses from endpoints.json
    /// Endpoints is the document root.
    /// </summary>
    public class Endpoints
    {
        /// <summary>
        /// File format version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Defined Partitions
        /// </summary>
        public IList<Partition> Partitions { get; set; }

        /// <summary>
        /// Load from file
        /// </summary>
        public static Endpoints Load(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return Load(stream);
            }
        }

        /// <summary>
        /// Load from stream
        /// </summary>
        public static Endpoints Load(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var json = reader.ReadToEnd();
                var endpoints = JsonConvert.DeserializeObject<Endpoints>(json);
                return endpoints;
            }
        }
    }
}
