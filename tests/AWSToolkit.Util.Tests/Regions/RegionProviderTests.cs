using System;
using System.Linq;
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
        private static readonly string LocalRegionId = $"{RegionProvider.LocalRegionIdPrefix}region";
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
        public void GetPartition()
        {
            InitializeAndWaitForUpdateEvents();

            Assert.Equal("amazonaws.com", _sut.GetPartition("aws").DnsSuffix);
            Assert.Equal("amazonaws.com.cn", _sut.GetPartition("aws-cn").DnsSuffix);
            Assert.Null(_sut.GetPartition("fake-partition"));
        }

        [Fact]
        public void GetPartitions()
        {
            InitializeAndWaitForUpdateEvents();

            Assert.Equal(2, _sut.GetPartitions().Count);
            Assert.Empty( _sut.GetPartitions().Where(x =>x.PartitionName.Contains("gov")).ToList());
        }

        [Fact]
        public void GetRegions()
        {
            InitializeAndWaitForUpdateEvents();

            var regions = _sut.GetRegions("aws");
            Assert.NotNull(regions);
            Assert.Contains(regions, r => r.Id == "us-east-1");
            Assert.Contains(regions, r => r.Id == $"{RegionProvider.LocalRegionIdPrefix}aws");

            var unexpectedPartitionRegions = _sut.GetRegions("fake-partition");
            Assert.NotNull(unexpectedPartitionRegions);
            Assert.Empty(unexpectedPartitionRegions);
        }

        [Fact]
        public void GetRegion()
        {
            InitializeAndWaitForUpdateEvents();

            var usEast = _sut.GetRegion("us-east-1");
            Assert.NotNull(usEast);
            Assert.Equal("us-east-1", usEast.Id);
            Assert.Equal("aws", usEast.PartitionId);
            Assert.Equal("US East (N. Virginia)", usEast.DisplayName);

            Assert.Null(_sut.GetRegion(null));
            var local = _sut.GetRegion($"{RegionProvider.LocalRegionIdPrefix}aws");
            Assert.NotNull(local);
            Assert.Equal($"{RegionProvider.LocalRegionIdPrefix}aws", local.Id);
            Assert.Equal("aws", local.PartitionId);
            Assert.Contains("Local", local.DisplayName);

            var unexpectedRegion = _sut.GetRegion("fake-partition");
            Assert.Null(unexpectedRegion);
        }

        [Fact]
        public void IsRegionLocal()
        {
            Assert.False(_sut.IsRegionLocal("us-east-1"));
            Assert.False(_sut.IsRegionLocal("fake-partition"));
            Assert.True(_sut.IsRegionLocal("toolkit-local-aws"));
        }

        [Fact]
        public void GetLocalEndpoint_UndefinedService()
        {
            Assert.Null(_sut.GetLocalEndpoint("foo-service"));
        }

        [Fact]
        public void SetAndGetLocalEndpoint()
        {
            var serviceName = "foo-service";

            _sut.SetLocalEndpoint(serviceName, "some-url");
            Assert.Equal("some-url", _sut.GetLocalEndpoint(serviceName));
        }

        [Theory]
        // Typical service in a region
        [InlineData("ec2", "us-east-1", true)]
        // Typical service in an alternate partition region
        [InlineData("ec2", "cn-northwest-1", true)]
        // Service that isn't available in all regions
        [InlineData("codeartifact", "us-east-1", true)]
        [InlineData("codeartifact", "ca-central-1", false)]
        // Global service
        [InlineData("iam", "us-east-1", true)]
        // Global service, alternate partition region
        [InlineData("iam", "cn-northwest-1", true)]
        // Unknown service
        [InlineData("fake-service", "us-east-1", false)]
        // Unknown region
        [InlineData("ec2", FakeRegionId, false)]
        public void IsServiceAvailable(string serviceName, string regionId, bool expectedResult)
        {
            InitializeAndWaitForUpdateEvents();

            Assert.Equal(expectedResult, _sut.IsServiceAvailable(serviceName, regionId));
        }

        [Fact]
        public void IsServiceAvailable_Local()
        {
            var serviceName = "foo-service";

            Assert.False(_sut.IsServiceAvailable(serviceName, LocalRegionId));

            _sut.SetLocalEndpoint(serviceName, "");
            Assert.False(_sut.IsServiceAvailable(serviceName, LocalRegionId));

            _sut.SetLocalEndpoint(serviceName, "some-url");
            Assert.True(_sut.IsServiceAvailable(serviceName, LocalRegionId));
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
