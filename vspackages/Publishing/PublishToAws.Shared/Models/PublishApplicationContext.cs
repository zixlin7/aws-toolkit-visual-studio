using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Publish.Package;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Settings;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Common components used by an instance (eg: document tab) of the Publish to AWS
    /// view. Each instance should have its own AWS Connection Manager, because
    /// the Credentials and Region are adjustable per view.
    /// 
    /// The intent of this class is to pass core components around without a lengthy
    /// parameter list.
    ///
    /// The contents of this class are intended to be mock-able.
    /// </summary>
    public class PublishApplicationContext
    {
        private readonly PublishContext _publishContext;

        public IPublishToAwsPackage PublishPackage => _publishContext.PublishPackage;
        public IRegionProvider RegionProvider => _publishContext.ToolkitContext.RegionProvider;
        public ITelemetryLogger TelemetryLogger => _publishContext.ToolkitContext.TelemetryLogger;
        public ICredentialManager CredentialManager => _publishContext.ToolkitContext.CredentialManager;
        public IAWSToolkitShellProvider ToolkitShellProvider => _publishContext.ToolkitShellProvider;

        public IPublishSettingsRepository PublishSettingsRepository => _publishContext.PublishSettingsRepository;

        public IAwsConnectionManager ConnectionManager { get; }

        public PublishApplicationContext(PublishContext publishContext)
            : this(publishContext, CreateConnectionManager(publishContext))
        {
        }

        public PublishApplicationContext(PublishContext publishContext, IAwsConnectionManager connectionManager)
        {
            _publishContext = publishContext;
            ConnectionManager = connectionManager;
        }

        /// <summary>
        /// Produces a Connection Manager from the Publish Context.
        /// Used to establish a connection manager that is separate from the
        /// Toolkit's main (AWS Explorer-based) Connection Manager.
        /// </summary>
        private static IAwsConnectionManager CreateConnectionManager(PublishContext context)
        {
            return new AwsConnectionManager(AwsConnectionManager.DefaultStsClientCreator,
                context.ToolkitContext.CredentialManager,
                context.ToolkitContext.TelemetryLogger,
                context.ToolkitContext.RegionProvider,
                new AppDataToolkitSettingsRepository());
        }
    }
}
