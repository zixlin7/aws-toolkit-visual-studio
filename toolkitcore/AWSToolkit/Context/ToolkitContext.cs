using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Context
{
    /// <summary>
    /// Core Toolkit components that most of the functionality should have access to.
    /// The intent of this class is to pass core components around without a lengthy
    /// parameter list. This also helps wean code away from singletons (ToolkitFactory.Instance)
    /// so that it can become more testable.
    ///
    /// The contents of this class are intended to be mockable.
    /// </summary>
    public class ToolkitContext
    {
        public IRegionProvider RegionProvider { get; set; }
        public IAwsServiceClientManager ServiceClientManager { get; set; }
        public ITelemetryLogger TelemetryLogger { get; set; }
        public IAwsConnectionManager ConnectionManager { get; set; }
        public ICredentialManager CredentialManager { get; set; }
        public ICredentialSettingsManager CredentialSettingsManager { get; set; }
        public IAWSToolkitShellProvider ToolkitHost { get; set; }
        public IToolkitHostInfo ToolkitHostInfo { get; set; }
    }
}
