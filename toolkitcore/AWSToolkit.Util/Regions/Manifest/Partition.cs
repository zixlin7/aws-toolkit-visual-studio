using System.Collections.Generic;
using Newtonsoft.Json;

namespace Amazon.AWSToolkit.Regions.Manifest
{
    /// <summary>
    /// Structure to deserialize what the Toolkit uses from endpoints.json
    /// Partition represents a collection of regions and services
    /// </summary>
    public class Partition
    {
        public string DnsSuffix { get; set; }

        /// <summary>
        /// Partition Id (eg: "aws")
        /// </summary>
        [JsonProperty("partition")]
        public string Id { get; set; }

        /// <summary>
        /// Friendly name (eg "AWS Standard")
        /// </summary>
        public string PartitionName { get; set; }

        /// <summary>
        /// Defines the partition's regions.
        /// Keyed by Region id (eg: "us-west-2")
        /// </summary>
        public Dictionary<string, Region> Regions { get; set; }

        /// <summary>
        /// Defines the services available in the partition.
        /// Keyed by <see cref="ServiceName"/>
        /// </summary>
        public Dictionary<string, Service> Services { get; set; }
    }
}
