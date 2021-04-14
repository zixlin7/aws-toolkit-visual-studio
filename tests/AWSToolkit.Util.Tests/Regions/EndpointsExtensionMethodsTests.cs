using System.Linq;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.Util.Tests.Resources;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class EndpointsExtensionMethodsTests
    {
        private const string EndpointsFilename = "sample-endpoints.json";
        private const string FakeRegionId = "us-moon-1";
        private readonly Endpoints _endpoints = Endpoints.Load(TestResources.LoadResourceFile(EndpointsFilename));

        [Fact]
        public void GetPartitionIdForRegion()
        {
            Assert.Equal("aws", _endpoints.GetPartitionIdForRegion("us-west-2"));
            Assert.Equal("aws-cn", _endpoints.GetPartitionIdForRegion("cn-north-1"));
            Assert.Null(_endpoints.GetPartitionIdForRegion(FakeRegionId));
        }

        [Theory]
        [InlineData("aws", "us-east-1")]
        [InlineData("aws-cn", "cn-north-1")]
        public void GetRegions(string partitionId, string expectedRegionId)
        {
            var regions = _endpoints.GetRegions(partitionId);
            Assert.NotNull(regions);

            var region = regions.FirstOrDefault(r => r.Id == expectedRegionId);
            Assert.NotNull(region);
            Assert.Equal(partitionId, region.PartitionId);
        }


        [Fact]
        public void GetPartition()
        {
            Assert.Equal("amazonaws.com",_endpoints.GetPartition("aws").DnsSuffix);
            Assert.Equal("amazonaws.com.cn", _endpoints.GetPartition("aws-cn").DnsSuffix);
            Assert.Null(_endpoints.GetPartition("fake-partition"));
        }

        [Fact]
        public void GetRegions_UnknownRegion()
        {
            var regions = _endpoints.GetRegions(FakeRegionId);
            Assert.NotNull(regions);
            Assert.Empty(regions);
        }

        [Fact]
        public void IsServiceAvailable()
        {
            Assert.True(_endpoints.IsServiceAvailable("ec2", "us-west-2"));
        }

        [Fact]
        public void IsServiceAvailable_GlobalRegion()
        {
            Assert.True(_endpoints.IsServiceAvailable("iam", "us-west-2"));
        }

        [Fact]
        public void IsServiceAvailable_UnknownRegion()
        {
            Assert.False(_endpoints.IsServiceAvailable("ec2", FakeRegionId));
        }
    }
}
