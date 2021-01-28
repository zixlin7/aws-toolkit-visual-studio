namespace Amazon.AWSToolkit.Regions
{
    public class ToolkitRegion
    {
        /// <summary>
        /// Region Id (eg: "us-west-2")
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// User friendly name (eg: "US West (Oregon)")
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Id of parent partition (eg: "aws")
        /// </summary>
        public string PartitionId { get; set; }
    }
}
