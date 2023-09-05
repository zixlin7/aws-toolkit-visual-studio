using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;

using Moq;

namespace Amazon.AWSToolkit.Tests.Common.Context
{
    /// <summary>
    /// Convenience class to create a starter set of ToolkitContext Mocks
    /// </summary>
    public class ToolkitContextFixture
    {
        public ToolkitContext ToolkitContext { get; }

        public TelemetryFixture TelemetryFixture { get; } = new TelemetryFixture();

        public Mock<IAwsConnectionManager> ConnectionManager { get; } = new Mock<IAwsConnectionManager>();
        public Mock<ICredentialManager> CredentialManager { get; } = new Mock<ICredentialManager>();
        public Mock<ICredentialSettingsManager> CredentialSettingsManager { get; } = new Mock<ICredentialSettingsManager>();
        public Mock<IRegionProvider> RegionProvider { get; } = new Mock<IRegionProvider>();
        public Mock<IAwsServiceClientManager> ServiceClientManager { get; } = new Mock<IAwsServiceClientManager>();
        public Mock<ITelemetryLogger> TelemetryLogger => TelemetryFixture.TelemetryLogger;
        public Mock<IAWSToolkitShellProvider> ToolkitHost { get; } = new Mock<IAWSToolkitShellProvider>();
        public Mock<IDialogFactory> DialogFactory { get; } = new Mock<IDialogFactory>();

        public ToolkitContextFixture()
        {
            ToolkitHost.Setup(mock => mock.GetDialogFactory()).Returns(DialogFactory.Object);

            ToolkitContext = new ToolkitContext()
            {
                ConnectionManager = ConnectionManager.Object,
                CredentialManager = CredentialManager.Object,
                CredentialSettingsManager = CredentialSettingsManager.Object,
                RegionProvider = RegionProvider.Object,
                ServiceClientManager = ServiceClientManager.Object,
                TelemetryLogger = TelemetryLogger.Object,
                ToolkitHost = ToolkitHost.Object
            };

            InitializeRegionProviderMocks();
        }

        private void InitializeRegionProviderMocks()
        {
            Partition mockPartition(string partitionId, string partitionName)
            {
                var partition = new Partition() { Id = partitionId, PartitionName = partitionName };

                RegionProvider.Setup(mock => mock.GetPartition(It.Is<string>(id => id == partitionId))).Returns(partition);

                return partition;
            }

            ToolkitRegion mockRegion(string partitionId, string regionId)
            {
                var region = new ToolkitRegion() { Id = regionId, DisplayName = regionId, PartitionId = partitionId };

                RegionProvider.Setup(mock => mock.GetRegion(It.Is<string>(id => id == regionId))).Returns(region);
                RegionProvider.Setup(mock => mock.GetPartitionId(It.Is<string>(id => id == regionId))).Returns(partitionId);

                return region;
            }

            var partitions = new List<Partition>();

            // Add partitions
            foreach (var pid in new List<string>() { PartitionIds.AWS, PartitionIds.AWS_CHINA, PartitionIds.AWS_GOV_CLOUD })
            {
                partitions.Add(mockPartition(pid, pid));

                var regions = new List<ToolkitRegion>();

                // Add default region
                if (pid == PartitionIds.DefaultPartitionId)
                {
                    regions.Add(mockRegion(PartitionIds.DefaultPartitionId, ToolkitRegion.DefaultRegionId));
                }

                // Add regions for current partition
                for (var r = 1; r <= 5; ++r)
                {
                    regions.Add(mockRegion(pid, $"region{r}-{pid}"));
                }

                // While order shouldn't be important, RegionProvider adds local host at the end, so do the same here
                regions.Add(mockRegion(pid, $"{Regions.RegionProvider.LocalRegionIdPrefix}-{pid}"));

                RegionProvider.Setup(mock => mock.GetRegions(It.Is<string>(id => id == pid))).Returns(regions);
            }

            RegionProvider.Setup(mock => mock.GetPartitions()).Returns(partitions);

            RegionProvider.Setup(mock => mock.IsRegionLocal(It.IsAny<string>())).Returns(false);
        }

        public void DefineCredentialProperties(ICredentialIdentifier credentialIdentifier, ProfileProperties profileProperties)
        {
            CredentialSettingsManager.Setup(m => m.GetProfileProperties(credentialIdentifier))
                .Returns(profileProperties);
        }

        public void SetupExecuteOnUIThread()
        {
            ToolkitHost.Setup(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()))
                .Callback<Action>(action => action());
        }

        public void SetupRegionAsLocal(string regionId)
        {
            RegionProvider.Setup(mock => mock.IsRegionLocal(regionId)).Returns(true);
        }

        public void DefineRegion(ToolkitRegion region)
        {
            RegionProvider.Setup(mock => mock.GetRegion(region.Id)).Returns(region);
        }

        public void SetupCreateServiceClient<TServiceClient>(TServiceClient client) where TServiceClient : class
        {
            ServiceClientManager.Setup(mock => mock.CreateServiceClient<TServiceClient>(
                It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns(client);

            ServiceClientManager.Setup(mock => mock.CreateServiceClient<TServiceClient>(
                It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>(), It.IsAny<ClientConfig>()))
                .Returns(client);
        }

        public void SetupGetToolkitCredentials(ToolkitCredentials credentials)
        {
            CredentialManager.Setup(
                mock => mock.GetToolkitCredentials(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns<ICredentialIdentifier, ToolkitRegion>((credentialId, region) => credentials);
        }

        public void DefineCredentialIdentifiers(IEnumerable<ICredentialIdentifier> credentialIdentifiers)
        {
            CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(credentialIdentifiers.ToList());
        }

        public void SetupCredentialManagerSupports(ICredentialIdentifier credentialIdentifier,
            AwsConnectionType connectionType, bool isSupported)
        {
            CredentialManager.Setup(mock => mock.Supports(credentialIdentifier, connectionType)).Returns(isSupported);
        }

        public void SetupConfirm(bool returnValue)
        {
            ToolkitHost.Setup(
                    mock => mock.Confirm(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>()))
                .Returns(returnValue);
        }
    }
}
