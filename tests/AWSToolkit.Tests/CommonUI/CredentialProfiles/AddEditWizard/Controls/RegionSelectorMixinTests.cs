using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard.Controls
{
    public class RegionSelectorMixinTests
    {
        private readonly RegionSelectorMixin _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public RegionSelectorMixinTests()
        {
            _sut = new RegionSelectorMixin(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public void LoadsPartitionsAndInitialRegionsOnCreation()
        {
            // Partitions
            Assert.NotEmpty(_sut.Partitions);
            Assert.Contains(_sut.Partitions, p => p.Id == PartitionIds.DefaultPartitionId);
            Assert.Equal(PartitionIds.DefaultPartitionId, _sut.SelectedPartition.Id);

            // Regions
            Assert.NotEmpty(_sut.Regions);
            Assert.Contains(_sut.Regions, r => r.Id == ToolkitRegion.DefaultRegionId);
            Assert.Equal(ToolkitRegion.DefaultRegionId, _sut.SelectedRegion.Id);
        }

        [Fact]
        public void SelectingDifferentPartitionLoadsDifferentRegions()
        {
            Assert.True(_sut.Partitions.Count() > 1, "Must have at least two partitions for this test.");

            var previousRegions = _sut.Regions;
            var nextPartition = _sut.Partitions.First(p => p.Id != _sut.SelectedPartition.Id);

            Assert.NotEqual(_sut.SelectedPartition, nextPartition);

            _sut.SelectedPartition = nextPartition;
            var nextRegions = _sut.Regions;

            Assert.NotEqual(RegionsToString(previousRegions), RegionsToString(nextRegions));           
        }

        [Fact]
        public void SettingSelectedRegionToExistingRegionSelectsThatRegion()
        {
            Assert.True(_sut.Regions.Count() > 1, "Must have at least two regions for this test.");

            var nextRegion = _sut.Regions.Last();

            Assert.NotEqual(_sut.SelectedRegion, nextRegion);

            _sut.SelectedRegion = nextRegion;

            Assert.Equal(_sut.SelectedRegion, nextRegion);
        }

        [Fact]
        public void SettingSelectedRegionToNonExistingRegionSelectsDefaultRegionIfAvailable()
        {
            var defaultPartition = _sut.Partitions.Single(p => p.Id == PartitionIds.DefaultPartitionId);
            var nextRegion = new ToolkitRegion();

            Assert.True(_sut.Regions.Count() > 1, "Must have at least two regions for this test.");
            Assert.True(defaultPartition != null, "Must have default partition for this test.");

            _sut.SelectedPartition = defaultPartition;

            Assert.NotEqual(_sut.SelectedRegion, nextRegion);

            _sut.SelectedRegion = nextRegion;

            Assert.Equal(_sut.SelectedRegion.Id, ToolkitRegion.DefaultRegionId);
        }

        [Fact]
        public void SettingSelectedRegionToNonExistingRegionSelectsFirstRegionIfDefaultNotAvailable()
        {
            var nonDefaultPartition = _sut.Partitions.First(p => p.Id != PartitionIds.DefaultPartitionId);
            var nextRegion = new ToolkitRegion();

            Assert.True(_sut.Regions.Count() > 1, "Must have at least two regions for this test.");

            _sut.SelectedPartition = nonDefaultPartition;

            Assert.NotEqual(_sut.SelectedRegion, nextRegion);

            _sut.SelectedRegion = nextRegion;

            Assert.NotEqual(_sut.SelectedRegion.Id, nextRegion.Id);
            Assert.NotEqual(_sut.SelectedRegion.Id, ToolkitRegion.DefaultRegionId);
        }

        private string RegionsToString(IEnumerable<ToolkitRegion> regions)
        {
            return string.Join(",", regions.Select(p => p.Id).OrderBy(id => id).ToArray());
        }
    }
}
