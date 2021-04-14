using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class AccountAndRegionPickerViewModelTestsFixture
    {
        /// <summary>
        /// Simulates that the service is available for any queried region
        /// </summary>
        public readonly List<string> SupportedServices = new List<string>();

        public const string SupportedServiceA = "serviceA";
        public const string PartitionA = "partitionA";
        public const string PartitionB = "partitionB";

        public readonly ToolkitRegion RegionA1 = new ToolkitRegion()
        {
            PartitionId = PartitionA,
            DisplayName = "Region a-1",
            Id = "a1",
        };
        public readonly ToolkitRegion RegionA2 = new ToolkitRegion()
        {
            PartitionId = PartitionA,
            DisplayName = "Region a-2",
            Id = "a2",
        };
        public readonly ToolkitRegion RegionB1 = new ToolkitRegion()
        {
            PartitionId = PartitionB,
            DisplayName = "Region b-1",
            Id = "b1",
        };

        private readonly Dictionary<string, List<ToolkitRegion>> _partitionRegionsMap = new Dictionary<string, List<ToolkitRegion>>();

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public readonly AccountAndRegionPickerViewModel ViewModel;

        public AccountAndRegionPickerViewModelTestsFixture()
        {
            _toolkitContextFixture.RegionProvider.Setup(mock => mock.GetRegions(It.IsAny<string>())).Returns<string>((partitionId) =>
            {
                if (!_partitionRegionsMap.TryGetValue(partitionId, out var regions))
                {
                    return new List<ToolkitRegion>();
                }

                return regions;
            });

            _toolkitContextFixture.RegionProvider.Setup(mock => mock.IsServiceAvailable(It.IsAny<string>(), It.IsAny<string>()))
                .Returns<string, string>((service, regionId) =>
                    {
                        return SupportedServices.Contains(service);
                    });

            AssociateRegionWithPartition(PartitionA, RegionA1);
            AssociateRegionWithPartition(PartitionA, RegionA2);
            AssociateRegionWithPartition(PartitionB, RegionB1);
            SupportedServices.Add(SupportedServiceA);

            ViewModel = new AccountAndRegionPickerViewModel(_toolkitContextFixture.ToolkitContext);
        }

        public void AssociateRegionWithPartition(string partitionId, ToolkitRegion region)
        {
            if (!_partitionRegionsMap.TryGetValue(partitionId, out var regions))
            {
                regions = new List<ToolkitRegion>();
                _partitionRegionsMap[partitionId] = regions;
            }

            regions.Add(region);
        }
    }

    public class AccountAndRegionPickerViewModelTests
    {
        private readonly AccountAndRegionPickerViewModelTestsFixture _fixture = new AccountAndRegionPickerViewModelTestsFixture();

        public AccountAndRegionPickerViewModelTests()
        {
        }

        [Fact]
        public void ShowRegions()
        {
            _fixture.ViewModel.ShowRegions(AccountAndRegionPickerViewModelTestsFixture.PartitionA);
            Assert.Equal(2, _fixture.ViewModel.Regions.Count);
            Assert.Contains(_fixture.RegionA1, _fixture.ViewModel.Regions);
            Assert.Contains(_fixture.RegionA2, _fixture.ViewModel.Regions);

            _fixture.ViewModel.ShowRegions(AccountAndRegionPickerViewModelTestsFixture.PartitionB);
            Assert.Single(_fixture.ViewModel.Regions);
            Assert.Contains(_fixture.RegionB1, _fixture.ViewModel.Regions);

            _fixture.ViewModel.ShowRegions("unknown-partition");
            Assert.Empty(_fixture.ViewModel.Regions);
        }

        [Fact]
        public void ShowRegions_SupportedServicesMix()
        {
            var unsupportedServiceName = "some-unsupported-service";

            _fixture.ViewModel.SetServiceFilter(new List<string>()
            {
                AccountAndRegionPickerViewModelTestsFixture.SupportedServiceA, unsupportedServiceName
            });
            _fixture.ViewModel.ShowRegions(AccountAndRegionPickerViewModelTestsFixture.PartitionA);
            Assert.Equal(2, _fixture.ViewModel.Regions.Count);
            Assert.Contains(_fixture.RegionA1, _fixture.ViewModel.Regions);
            Assert.Contains(_fixture.RegionA2, _fixture.ViewModel.Regions);
        }

        [Fact]
        public void ShowRegions_UnsupportedServices()
        {
            var unsupportedServiceName = "some-unsupported-service";

            _fixture.ViewModel.SetServiceFilter(new List<string>() {unsupportedServiceName});
            _fixture.ViewModel.ShowRegions(AccountAndRegionPickerViewModelTestsFixture.PartitionA);
            Assert.Empty(_fixture.ViewModel.Regions);
        }

        [Fact]
        public void GetRegion()
        {
            _fixture.ViewModel.SetServiceFilter(new List<string>());
            _fixture.ViewModel.ShowRegions(AccountAndRegionPickerViewModelTestsFixture.PartitionA);

            Assert.Equal(_fixture.RegionA1, _fixture.ViewModel.GetRegion(_fixture.RegionA1.Id));
            Assert.Null(_fixture.ViewModel.GetRegion("unknown-region"));
            Assert.Null(_fixture.ViewModel.GetRegion(null));
        }

        [Fact]
        public void GetMostRecentRegionId()
        {
            _fixture.ViewModel.PartitionId = AccountAndRegionPickerViewModelTestsFixture.PartitionA;
            _fixture.ViewModel.Region = _fixture.RegionA1;
            _fixture.ViewModel.Region = _fixture.RegionA2;
            _fixture.ViewModel.PartitionId = AccountAndRegionPickerViewModelTestsFixture.PartitionB;
            _fixture.ViewModel.Region = _fixture.RegionB1;

            Assert.Equal(_fixture.RegionA2.Id, _fixture.ViewModel.GetMostRecentRegionId(AccountAndRegionPickerViewModelTestsFixture.PartitionA));
            Assert.Equal(_fixture.RegionB1.Id, _fixture.ViewModel.GetMostRecentRegionId(AccountAndRegionPickerViewModelTestsFixture.PartitionB));
        }

        [Fact]
        public void GetMostRecentRegionId_UnusedPartitionId()
        {
            _fixture.ViewModel.PartitionId = AccountAndRegionPickerViewModelTestsFixture.PartitionA;
            _fixture.ViewModel.Region = _fixture.RegionA2;

            Assert.Null(_fixture.ViewModel.GetMostRecentRegionId(AccountAndRegionPickerViewModelTestsFixture.PartitionB));
            Assert.Null(_fixture.ViewModel.GetMostRecentRegionId(null));
        }
    }
}
