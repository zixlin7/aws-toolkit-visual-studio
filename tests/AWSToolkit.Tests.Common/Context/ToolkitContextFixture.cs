using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
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

        public Mock<ICredentialManager> CredentialManager { get; } = new Mock<ICredentialManager>();
        public Mock<ICredentialSettingsManager> CredentialSettingsManager { get; } = new Mock<ICredentialSettingsManager>();
        public Mock<IRegionProvider> RegionProvider { get; } = new Mock<IRegionProvider>();
        public Mock<IAwsServiceClientManager> ServiceClientManager { get; } = new Mock<IAwsServiceClientManager>();
        public Mock<ITelemetryLogger> TelemetryLogger { get; } = new Mock<ITelemetryLogger>();

        public ToolkitContextFixture()
        {
            ToolkitContext = new ToolkitContext()
            {
                CredentialManager = CredentialManager.Object,
                CredentialSettingsManager = CredentialSettingsManager.Object,
                RegionProvider = RegionProvider.Object,
                ServiceClientManager = ServiceClientManager.Object,
                TelemetryLogger = TelemetryLogger.Object,
            };
        }
    }
}
