namespace Amazon.AWSToolkit.Regions
{
    /// <summary>
    /// Represents partition ids for use in scenarios where Toolkit supports
    /// some capabilities for certain partitions only.
    /// 
    /// This class is not ideal for future maintenance and might be revisited later.
    /// </summary>
    public static class PartitionIds
    {
        public const string DefaultPartitionId = AWS;

        public const string AWS = "aws";
        public const string AWS_CHINA = "aws-cn";
        public const string AWS_GOV_CLOUD = "aws-us-gov";
    }
}
