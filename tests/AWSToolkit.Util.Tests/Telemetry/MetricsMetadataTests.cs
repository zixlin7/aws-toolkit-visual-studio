using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class MetricsMetadataTests
    {
        [Theory]
        [InlineData("account12345", "account12345")]
        [InlineData(null, MetadataValue.NotSet)]
        [InlineData("", MetadataValue.NotSet)]
        public void AccountIdOrDefault(string accountId, string expectedResult)
        {
            Assert.Equal(expectedResult, MetricsMetadata.AccountIdOrDefault(accountId));
        }

        [Theory]
        [InlineData("us-west", "us-west")]
        [InlineData(null, MetadataValue.NotSet)]
        [InlineData("", MetadataValue.NotSet)]
        public void RegionOrDefault(string regionId, string expectedResult)
        {
            Assert.Equal(expectedResult, MetricsMetadata.RegionOrDefault(regionId));
        }

        [Fact]
        public void RegionOrDefault_ToolkitRegion()
        {
            var region = new ToolkitRegion()
            {
                Id = "us-west",
            };
            Assert.Equal(region.Id, MetricsMetadata.RegionOrDefault(region));
        }

        [Fact]
        public void RegionOrDefault_NullToolkitRegion()
        {
            Assert.Equal(MetadataValue.NotSet, MetricsMetadata.RegionOrDefault((ToolkitRegion)null));
        }
    }
}
