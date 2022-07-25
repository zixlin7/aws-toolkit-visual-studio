using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Moq;
using Xunit;

namespace AWSToolkitPackage.Tests.Navigator
{
    public class NavigatorViewModelTests
    {
        private readonly Mock<IRegionProvider> _regionProvider = new Mock<IRegionProvider>();
        private readonly NavigatorViewModel _sut;

        private const string PartitionA = "partitionA";
        private const string PartitionB = "partitionB";

        private readonly ToolkitRegion _regionA1 = new ToolkitRegion()
        {
            PartitionId = PartitionA, DisplayName = "Region a-1", Id = "a1",
        };
        private readonly ToolkitRegion _regionA2 = new ToolkitRegion()
        {
            PartitionId = PartitionA, DisplayName = "Region a-2", Id = "a2",
        };
        private readonly ToolkitRegion _regionB1 = new ToolkitRegion()
        {
            PartitionId = PartitionB, DisplayName = "Region b-1", Id = "b1",
        };

        private readonly Dictionary<string, List<ToolkitRegion>> _partitionRegionsMap = new Dictionary<string, List<ToolkitRegion>>();

        public NavigatorViewModelTests()
        {
            _partitionRegionsMap.Add(PartitionA, new List<ToolkitRegion>() {_regionA1, _regionA2});
            _partitionRegionsMap.Add(PartitionB, new List<ToolkitRegion>() {_regionB1});

            _regionProvider.Setup(mock => mock.GetRegions(It.IsAny<string>())).Returns<string>((partitionId) =>
            {
                if (!_partitionRegionsMap.TryGetValue(partitionId, out var regions))
                {
                    return new List<ToolkitRegion>();
                }

                return regions;
            });

            _sut = new NavigatorViewModel(_regionProvider.Object);
        }

        [Fact]
        public void ShowRegionsForPartition()
        {
            _sut.ShowRegionsForPartition(PartitionA);
            Assert.Equal(2, _sut.Regions.Count);
            Assert.Contains(_regionA1, _sut.Regions);
            Assert.Contains(_regionA2, _sut.Regions);

            _sut.ShowRegionsForPartition(PartitionB);
            Assert.Single(_sut.Regions);
            Assert.Contains(_regionB1, _sut.Regions);

            _sut.ShowRegionsForPartition("unknown-partition");
            Assert.Empty(_sut.Regions);
        }

        [Fact]
        public void GetRegion()
        {
            _sut.ShowRegionsForPartition(PartitionA);

            Assert.Equal(_regionA1, _sut.GetRegion(_regionA1.Id));
            Assert.Null(_sut.GetRegion("unknown-region"));
            Assert.Null(_sut.GetRegion(null));
        }

        [Fact]
        public void GetMostRecentRegionId()
        {
            _sut.PartitionId = PartitionA;
            _sut.Region = _regionA1;
            _sut.Region = _regionA2;
            _sut.PartitionId = PartitionB;
            _sut.Region = _regionB1;

            Assert.Equal(_regionA2.Id, _sut.GetMostRecentRegionId(PartitionA));
            Assert.Equal(_regionB1.Id, _sut.GetMostRecentRegionId(PartitionB));
        }

        [Fact]
        public void GetMostRecentRegionId_UnusedPartitionId()
        {
            _sut.PartitionId = PartitionA;
            _sut.Region = _regionA2;

            Assert.Null(_sut.GetMostRecentRegionId(PartitionB));
            Assert.Null(_sut.GetMostRecentRegionId(null));
        }
    }
}
