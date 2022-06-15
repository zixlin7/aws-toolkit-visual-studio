using Amazon.AWSToolkit.Regions;
using Amazon.AwsToolkit.Telemetry.Events.Core;

namespace Amazon.AWSToolkit.Telemetry
{
    public static class MetricsMetadata
    {
        public static string AccountIdOrDefault(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return MetadataValue.NotSet;
            }

            return accountId;
        }

        public static string RegionOrDefault(ToolkitRegion region)
        {
            return RegionOrDefault(region?.Id);
        }

        public static string RegionOrDefault(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return MetadataValue.NotSet;
            }

            return region;
        }
    }
}
