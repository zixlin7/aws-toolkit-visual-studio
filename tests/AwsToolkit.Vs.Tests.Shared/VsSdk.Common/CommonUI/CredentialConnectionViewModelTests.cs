using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using AwsToolkit.VsSdk.Common.CommonUI.Models;

using Moq;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class CredentialConnectionViewModelTests
    {
        private static readonly ICredentialIdentifier SampleCredentialId =
            FakeCredentialIdentifier.Create("sample-profile");

        public const string PartitionA = "partitionA";
        public const string PartitionB = "partitionB";

        private readonly CredentialConnectionViewModel _viewModel;
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private static readonly ToolkitRegion LocalRegion = new ToolkitRegion()
        {
            DisplayName = "local", Id = "local",
        };

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

        public CredentialConnectionViewModelTests()
        {
            SetupGetRegions();
            SetupPartitionsWithRegions();
            SetupCredentialIds();
            _viewModel = new CredentialConnectionViewModel(_toolkitContextFixture.ToolkitContext);
        }

        private void SetupCredentialIds()
        {
            _toolkitContextFixture.DefineCredentialIdentifiers(new[] { SampleCredentialId });
            _toolkitContextFixture.SetupCredentialManagerSupports(SampleCredentialId, AwsConnectionType.AwsCredentials,
                true);
            _toolkitContextFixture.SetupCredentialManagerSupports(SampleCredentialId, AwsConnectionType.AwsToken,
                false);
        }

        private void SetupPartitionsWithRegions()
        {
            AssociateRegionWithPartition(PartitionA, RegionA1);
            AssociateRegionWithPartition(PartitionA, RegionA2);
            AssociateRegionWithPartition(PartitionA, LocalRegion);
            AssociateRegionWithPartition(PartitionB, RegionB1);
            AssociateRegionWithPartition(PartitionB, LocalRegion);
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

            _toolkitContextFixture.RegionProvider.Setup(mock => mock.IsRegionLocal(It.IsAny<string>()))
                .Returns<string>(regionId => LocalRegion.Id == regionId);
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
        public void UpdateRegionForPartition()
        {
            _viewModel.IncludeLocalRegions = true;

            _viewModel.PartitionId = PartitionA;
            _viewModel.UpdateRegionForPartition();
            Assert.Equal(3, _viewModel.Regions.Count);
            Assert.Contains(RegionA1, _viewModel.Regions);
            Assert.Contains(RegionA2, _viewModel.Regions);
            Assert.Contains(LocalRegion, _viewModel.Regions);
            Assert.Equal(LocalRegion, _viewModel.Region);

            _viewModel.PartitionId = PartitionB;
            _viewModel.UpdateRegionForPartition();
            Assert.Equal(2, _viewModel.Regions.Count);
            Assert.Contains(RegionB1, _viewModel.Regions);
            Assert.Contains(LocalRegion, _viewModel.Regions);
            Assert.Equal(LocalRegion, _viewModel.Region);

            _viewModel.PartitionId = "unknown-partition";
            _viewModel.UpdateRegionForPartition();
            Assert.Empty(_viewModel.Regions);
            Assert.Null(_viewModel.Region);
        }


        [Fact]
        public void UpdateRegionForPartition_NoLocal()
        {
            _viewModel.IncludeLocalRegions = false;

            _viewModel.PartitionId = PartitionA;
            _viewModel.UpdateRegionForPartition();
            Assert.Equal(2, _viewModel.Regions.Count);
            Assert.Contains(RegionA1, _viewModel.Regions);
            Assert.Contains(RegionA2, _viewModel.Regions);
            Assert.Equal(RegionA1, _viewModel.Region);

            _viewModel.PartitionId = PartitionB;
            _viewModel.UpdateRegionForPartition();
            Assert.Single(_viewModel.Regions);
            Assert.Contains(RegionB1, _viewModel.Regions);
            Assert.Equal(RegionB1, _viewModel.Region);

            _viewModel.PartitionId = "unknown-partition";
            _viewModel.UpdateRegionForPartition();
            Assert.Empty(_viewModel.Regions);
            Assert.Null(_viewModel.Region);
        }


        [Fact]
        public void GetRegion()
        {
            _viewModel.PartitionId = PartitionA;
            _viewModel.UpdateRegionForPartition();

            Assert.Equal(RegionA1, _viewModel.GetRegion(RegionA1.Id));
            Assert.Null(_viewModel.GetRegion("unknown-region"));
            Assert.Null(_viewModel.GetRegion(null));
        }


        public static IEnumerable<object[]> ConnectionStatusData = new List<object[]>
        {
            new object[]
            {
                new ConnectionState.IncompleteConfiguration(SampleCredentialId, null),
                CredentialConnectionStatus.Info, false, false
            },
            new object[] { new ConnectionState.InvalidConnection(null), CredentialConnectionStatus.Error, false, true },
            new object[]
            {
                new ConnectionState.ValidConnection(SampleCredentialId, RegionA1), CredentialConnectionStatus.Info,
                true, false
            },
            new object[] { new ConnectionState.ValidatingConnection(), CredentialConnectionStatus.Info, false, false },
            new object[] { new ConnectionState.InitializingToolkit(), CredentialConnectionStatus.Info, false, false },
        };


        [Theory]
        [MemberData(nameof(ConnectionStatusData))]
        public void UpdateRequiredConnectionProperties(ConnectionState connectionState,
            CredentialConnectionStatus expectedStatus, bool expectedIsValid, bool expectedIsBad)
        {
            _viewModel.UpdateRequiredConnectionProperties(connectionState);
            Assert.Equal(expectedStatus, _viewModel.ConnectionStatus);
            Assert.Equal(expectedIsValid, _viewModel.IsConnectionValid);
            Assert.Equal(expectedIsBad, _viewModel.IsConnectionBad);
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
            _toolkitContextFixture.DefineCredentialProperties(SampleCredentialId,
                new ProfileProperties() { Region = associatedRegion, });
        }

        [Fact]
        public void GetCredentialIdentifiers_NoConnectionTypes()
        {
            Assert.Contains(SampleCredentialId, _viewModel.GetCredentialIdentifiers());
        }

        [Fact]
        public void GetCredentialIdentifiers_SupportedConnectionTypes()
        {
            _viewModel.ConnectionTypes.Add(AwsConnectionType.AwsCredentials);
            Assert.Contains(SampleCredentialId, _viewModel.GetCredentialIdentifiers());

            // Test when the list contains more than one type
            _viewModel.ConnectionTypes.Add(AwsConnectionType.AwsToken);
            Assert.Contains(SampleCredentialId, _viewModel.GetCredentialIdentifiers());
        }

        [Fact]
        public void GetCredentialIdentifiers_NoSupportedConnectionTypes()
        {
            _viewModel.ConnectionTypes.Add(AwsConnectionType.AwsToken);
            Assert.DoesNotContain(SampleCredentialId, _viewModel.GetCredentialIdentifiers());
        }
    }
}
