using System;
using System.Threading;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AWSToolkit.Util.Tests.Resources;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class RegionProviderTests : IDisposable
    {
        private const string EndpointsFilename = "sample-endpoints.json";
        private const string FakeRegionId = "us-moon-1";
        private readonly Mock<IResourceFetcher> _sampleEndpointsFetcher = new Mock<IResourceFetcher>();
        private readonly RegionProvider _sut;

        public RegionProviderTests()
        {
            _sampleEndpointsFetcher.Setup(mock => mock.Get(It.IsAny<string>()))
                .Returns(() => TestResources.LoadResourceFile(EndpointsFilename));

            _sut = new RegionProvider(_sampleEndpointsFetcher.Object, _sampleEndpointsFetcher.Object);
        }

        [Fact]
        public void Initialize()
        {
            InitializeAndWaitForUpdateEvents();

            _sampleEndpointsFetcher.Verify(mock => mock.Get(RegionProvider.EndpointsFile), Times.Exactly(2));
        }

        [Fact]
        public void InitializeUpdatesAwsSdk()
        {
            // Sample endpoints file contains a test service entry in us-west-2
            // check that this service is available after a Reload
            var testServiceRegionId = "us-west-2";
            var testServiceName = "test-service";
            var testServiceHostname = "test-hostname";

            // Sample endpoints file does not contain govcloud partition data
            // check that a govcloud region isn't available after a Reload
            var testPartition = "aws-us-gov";
            var testRegion = "us-gov-east-1";

            var beforeReloadRegion = RegionEndpoint.GetBySystemName(testRegion);
            Assert.Equal(testPartition, beforeReloadRegion.PartitionName);
            Assert.NotEqual("Unknown", beforeReloadRegion.DisplayName);

            var beforeReloadEndpoints = RegionEndpoint.GetBySystemName(testServiceRegionId)
                .GetEndpointForService(testServiceName);
            Assert.DoesNotContain(testServiceHostname, beforeReloadEndpoints.Hostname);

            InitializeAndWaitForUpdateEvents();

            var afterReloadRegion = RegionEndpoint.GetBySystemName(testRegion);
            Assert.NotEqual(testPartition, afterReloadRegion.PartitionName);
            Assert.Equal("Unknown", afterReloadRegion.DisplayName);

            var afterReloadEndpoints = RegionEndpoint.GetBySystemName(testServiceRegionId)
                .GetEndpointForService(testServiceName);
            Assert.Contains(testServiceHostname, afterReloadEndpoints.Hostname);
        }

        [Fact]
        public void GetPartitionId()
        {
            InitializeAndWaitForUpdateEvents();

            Assert.Equal("aws", _sut.GetPartitionId("us-west-2"));
            Assert.Equal("aws-cn", _sut.GetPartitionId("cn-north-1"));
            Assert.Null(_sut.GetPartitionId(FakeRegionId));
        }

        [Fact]
        public void GetRegions()
        {
            InitializeAndWaitForUpdateEvents();

            var regions = _sut.GetRegions("aws");
            Assert.NotNull(regions);
            Assert.Contains(regions, r => r.Id == "us-east-1");

            var unexpectedPartitionRegions = _sut.GetRegions("fake-partition");
            Assert.NotNull(unexpectedPartitionRegions);
            Assert.Empty(unexpectedPartitionRegions);
        }

        public void Dispose()
        {
            // "Reset" the SDK's endpoints data because these tests mess with it
            RegionEndpoint.Reload(null);
        }

        private void InitializeAndWaitForUpdateEvents()
        {
            var syncHandle = new ManualResetEvent(false);
            int timesCalled = 0;

            _sut.RegionProviderUpdated += (sender, args) =>
            {
                timesCalled++;
                if (timesCalled == 2)
                {
                    // We expect one callback for each of the two fetchers (one local, one remote)
                    syncHandle.Set();
                }
            };

            _sut.Initialize();
            syncHandle.WaitOne();
        }
    }
}
