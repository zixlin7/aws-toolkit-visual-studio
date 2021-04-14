using Amazon.AWSToolkit.Regions.Manifest;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class ServiceTests
    {
        [Fact]
        public void IsGlobal()
        {
            Assert.False(new Service()
            {
                IsRegionalized = null,
                PartitionEndpoint = null,
            }.IsGlobal());

            Assert.False(new Service()
            {
                IsRegionalized = null,
                PartitionEndpoint = "endpoint",
            }.IsGlobal());

            Assert.False(new Service()
            {
                IsRegionalized = true,
                PartitionEndpoint = null,
            }.IsGlobal());

            Assert.False(new Service()
            {
                IsRegionalized = true,
                PartitionEndpoint = "endpoint",
            }.IsGlobal());

            Assert.False(new Service()
            {
                IsRegionalized = false,
                PartitionEndpoint = null,
            }.IsGlobal());

            Assert.True(new Service()
            {
                IsRegionalized = false,
                PartitionEndpoint = "endpoint",
            }.IsGlobal());
        }
    }
}
