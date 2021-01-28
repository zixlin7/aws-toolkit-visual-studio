namespace Amazon.AWSToolkit.Regions.Manifest
{
    /// <summary>
    /// Structure to deserialize what the Toolkit uses from endpoints.json
    /// </summary>
    public class Region
    {
        /// <summary>
        /// Friendly name (eg: "US West (Oregon)")
        /// </summary>
        public string Description { get; set; }
    }
}
