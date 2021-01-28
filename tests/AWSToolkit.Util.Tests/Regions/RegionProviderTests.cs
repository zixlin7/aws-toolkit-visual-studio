using System.Threading;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.ResourceFetchers;
using Amazon.AWSToolkit.Util.Tests.Resources;
using Moq;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Regions
{
    public class RegionProviderTests
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
