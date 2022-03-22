using System;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
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

        public Mock<ICredentialManager> CredentialManager { get; } = new Mock<ICredentialManager>();
        public Mock<ICredentialSettingsManager> CredentialSettingsManager { get; } = new Mock<ICredentialSettingsManager>();
        public Mock<IRegionProvider> RegionProvider { get; } = new Mock<IRegionProvider>();
        public Mock<IAwsServiceClientManager> ServiceClientManager { get; } = new Mock<IAwsServiceClientManager>();
        public Mock<ITelemetryLogger> TelemetryLogger => TelemetryFixture.TelemetryLogger;
        public Mock<IAWSToolkitShellProvider> ToolkitHost { get; } = new Mock<IAWSToolkitShellProvider>();

        public ToolkitContextFixture()
        {
            ToolkitContext = new ToolkitContext()
            {
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
    }
}
