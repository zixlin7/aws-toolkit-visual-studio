using System.Collections.Generic;

namespace Amazon.AWSToolkit.Regions.Manifest
{
    /// <summary>
    /// Structure to deserialize what the Toolkit uses from endpoints.json
    /// </summary>
    public class Service
    {
        /// <summary>
        /// Keyed by Region id (eg: "us-west-2")
        /// The dictionary values are a type 'object' because the toolkit
        /// doesn't explicitly use that data at this time.
        /// </summary>
        public Dictionary<string, object> Endpoints { get; set; }
    }
}
