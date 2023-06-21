using Amazon.AWSToolkit.CommonUI.CredentialProfiles;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles
{
    public class RegionSelectorViewModelTests
    {
        private string _regionId;

        private readonly RegionSelectorViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public RegionSelectorViewModelTests()
        {
            _sut = new RegionSelectorViewModel(_toolkitContextFixture.ToolkitContext, () => _regionId, v => _regionId = v);
        }

        [Fact]
        public void RegionIdGetterSetterSanityCheck()
        {
            const string expected = "us-best-42";

            _regionId = null;

            Assert.Null(_sut.SelectedRegionId);

            _sut.SelectedRegionId = expected;
            Assert.Equal(expected, _sut.SelectedRegionId);
        }

        [Fact]
        public void DefaultPartitionSelectedAndRegionsLoadedWhenRegionNotSet()
        {
            Assert.Equal(PartitionIds.DefaultPartitionId, _sut.SelectedPartitionId);
            Assert.NotEmpty(_sut.Partitions);
            Assert.NotEmpty(_sut.Regions);
        }

        [Fact]
        public void SelectingPartitionLoadsRegions()
        {
            _sut.SelectedPartitionId = PartitionIds.AWS_CHINA;
            foreach (var region in _sut.Regions)
            {
                Assert.Equal(PartitionIds.AWS_CHINA, region.PartitionId);
            }
        }

        [Fact]
        public void SelectingNoPartitionLoadsNoRegions()
        {
            _sut.SelectedPartitionId = null;
            Assert.Empty(_sut.Regions);
        }

        [Fact]
        public void SettingRegionSetsPartitionContainingRegion()
        {
            var regionId = GetRegionId(PartitionIds.AWS_GOV_CLOUD, 3);
            _sut.SelectedRegionId = regionId;
            Assert.Equal(regionId, _sut.SelectedRegionId);
            Assert.Contains(_sut.Regions, r => regionId == r.Id);

            Assert.Equal(PartitionIds.AWS_GOV_CLOUD, _sut.SelectedPartitionId);
        }

        [Fact]
        public void SelectingPartitionNotContainingRegionSetsRegionToDefaultOrFirst()
        {
            var regionId = GetRegionId(PartitionIds.AWS, 3);
            _sut.SelectedRegionId = regionId;
            Assert.Equal(regionId, _sut.SelectedRegionId);

            _sut.SelectedPartitionId = PartitionIds.AWS_CHINA;
            Assert.Equal(PartitionIds.AWS_CHINA, _sut.SelectedPartitionId);
            Assert.Equal(GetRegionId(PartitionIds.AWS_CHINA, 1), _sut.SelectedRegionId);
        }

        private static string GetRegionId(string partitionId, int regionIndex)
        {
            return $"region{regionIndex}-{partitionId}";
        }
    }
}
