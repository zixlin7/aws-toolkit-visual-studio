using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialSelector
{
    public class CredentialSelectionViewModelTests
    {
        private static readonly ICredentialIdentifier SampleCredentialId = FakeCredentialIdentifier.Create("sample-profile");
        public const string PartitionA = "partitionA";
        public const string PartitionB = "partitionB";

        private readonly CredentialSelectionViewModel _viewModel;
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private static readonly ToolkitRegion RegionA1 = new ToolkitRegion()
        {
            PartitionId = PartitionA, DisplayName = "Region a-1", Id = "a1",
        };

        private readonly ToolkitRegion RegionA2 = new ToolkitRegion()
        {
            PartitionId = PartitionA, DisplayName = "Region a-2", Id = "a2",
        };

        private readonly ToolkitRegion RegionB1 = new ToolkitRegion()
        {
            PartitionId = PartitionB, DisplayName = "Region b-1", Id = "b1",
        };

        private readonly Dictionary<string, List<ToolkitRegion>> _partitionRegionsMap =
            new Dictionary<string, List<ToolkitRegion>>();

        public CredentialSelectionViewModelTests()
        {
            SetupGetRegions();
            SetupPartitionsWithRegions();
            _viewModel = new CredentialSelectionViewModel(_toolkitContextFixture.ToolkitContext);
        }

        private void SetupPartitionsWithRegions()
        {
            AssociateRegionWithPartition(PartitionA, RegionA1);
            AssociateRegionWithPartition(PartitionA, RegionA2);
            AssociateRegionWithPartition(PartitionB, RegionB1);
        }

        private void SetupGetRegions()
        {
            _toolkitContextFixture.RegionProvider.Setup(mock => mock.GetRegions(It.IsAny<string>())).Returns<string>(
                (partitionId) =>
                {
                    if (!_partitionRegionsMap.TryGetValue(partitionId, out var regions))
                    {
                        return new List<ToolkitRegion>();
                    }

                    return regions;
                });
        }

        private void AssociateRegionWithPartition(string partitionId, ToolkitRegion region)
        {
            if (!_partitionRegionsMap.TryGetValue(partitionId, out var regions))
            {
                regions = new List<ToolkitRegion>();
                _partitionRegionsMap[partitionId] = regions;
            }

            regions.Add(region);
        }

        [Fact]
        public void ShowRegions()
        {
            _viewModel.ShowRegions(PartitionA);
            Assert.Equal(2, _viewModel.Regions.Count);
            Assert.Contains(RegionA1, _viewModel.Regions);
            Assert.Contains(RegionA2, _viewModel.Regions);

            _viewModel.ShowRegions(PartitionB);
            Assert.Single(_viewModel.Regions);
            Assert.Contains(RegionB1, _viewModel.Regions);

            _viewModel.ShowRegions("unknown-partition");
            Assert.Empty(_viewModel.Regions);
        }


        [Fact]
        public void GetRegion()
        {
            _viewModel.ShowRegions(PartitionA);

            Assert.Equal(RegionA1, _viewModel.GetRegion(RegionA1.Id));
            Assert.Null(_viewModel.GetRegion("unknown-region"));
            Assert.Null(_viewModel.GetRegion(null));
        }

        [Fact]
        public void GetMostRecentRegionId()
        {
            _viewModel.PartitionId = PartitionA;
            _viewModel.Region = RegionA1;
            _viewModel.Region = RegionA2;
            _viewModel.PartitionId = PartitionB;
            _viewModel.Region = RegionB1;

            Assert.Equal(RegionA2.Id, _viewModel.GetMostRecentRegionId(PartitionA));
            Assert.Equal(RegionB1.Id, _viewModel.GetMostRecentRegionId(PartitionB));
        }

        [Fact]
        public void GetMostRecentRegionId_UnusedPartitionId()
        {
            _viewModel.PartitionId = PartitionA;
            _viewModel.Region = RegionA2;

            Assert.Null(_viewModel.GetMostRecentRegionId(PartitionB));
            Assert.Null(_viewModel.GetMostRecentRegionId(null));
        }


        public static IEnumerable<object[]> ConnectionStatusData = new List<object[]>
        {
            new object[]
            {
                new ConnectionState.IncompleteConfiguration(SampleCredentialId, null),
                CredentialConnectionStatus.Info
            },
            new object[] { new ConnectionState.InvalidConnection(null), CredentialConnectionStatus.Error },
            new object[]
            {
                new ConnectionState.ValidConnection(SampleCredentialId, RegionA1), CredentialConnectionStatus.Info
            },
            new object[] { new ConnectionState.ValidatingConnection(), CredentialConnectionStatus.Info },
            new object[] { new ConnectionState.InitializingToolkit(), CredentialConnectionStatus.Info },
        };

        [Theory]
        [MemberData(nameof(ConnectionStatusData))]
        public void GetConnectionStatus(ConnectionState connectionState, CredentialConnectionStatus expectedStatus)
        {
            var actualStatus = _viewModel.GetConnectionStatus(connectionState);
            Assert.Equal(expectedStatus, actualStatus);
        }

        [Fact]
        public void GetAssociatedRegionId()
        {
            var associatedRegion = "associated-region";
            DefineCredentialsRegion(associatedRegion);

            Assert.Equal(associatedRegion, _viewModel.GetAssociatedRegionId(SampleCredentialId));
        }

        [Fact]
        public void GetAssociatedRegionId_Cached()
        {
            var associatedRegion = "associated-region";
            DefineCredentialsRegion(associatedRegion);

            Assert.Equal(associatedRegion, _viewModel.GetAssociatedRegionId(SampleCredentialId));
            Assert.Equal(associatedRegion, _viewModel.GetAssociatedRegionId(SampleCredentialId));

            _toolkitContextFixture.CredentialSettingsManager
                .Verify(m => m.GetProfileProperties(It.IsAny<ICredentialIdentifier>()), Times.Once);
        }

        private void DefineCredentialsRegion(string associatedRegion)
        {
            _toolkitContextFixture.DefineCredentialProperties(SampleCredentialId, new ProfileProperties()
            {
                Region = associatedRegion,
            });
        }
    }
}
