namespace Amazon.AWSToolkit.Regions
{
    /// <summary>
    /// Service names as referenced in the endpoints.json manifest
    /// </summary>
    public class ServiceName
    {
        public static readonly ServiceName S3 = new ServiceName("s3");
        public static readonly ServiceName Ec2 = new ServiceName("ec2");
        public static readonly ServiceName Ecr = new ServiceName("ecr");
        public static readonly ServiceName Ecs = new ServiceName("ecs");
        // TODO : populate with more definitions from RegionEndpointsManager when migrating to IRegionProvider

        public string Value { get; }

        private ServiceName(string value) { Value = value; }
    }
}
