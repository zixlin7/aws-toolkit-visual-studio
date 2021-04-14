using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.Runtime;
using Amazon.SecurityToken;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public class ToolkitCredentialInitializer: IDisposable
    {
        private readonly ITelemetryLogger _telemetryLogger;
        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly AwsConnectionManager _awsConnectionManager;
        private readonly CredentialManager _credentialManager;
        private PersistConnectionSettings _persistConnectionSettings;
        private readonly Dictionary<string, ICredentialProviderFactory> _factoryMap = new Dictionary<string, ICredentialProviderFactory>();
        public IAwsConnectionManager AwsConnectionManager => _awsConnectionManager;
        public ICredentialManager CredentialManager => _awsConnectionManager?.CredentialManager;
        public ICredentialSettingsManager CredentialSettingsManager => _awsConnectionManager?.CredentialManager?.CredentialSettingsManager;

        public ToolkitCredentialInitializer(ITelemetryLogger telemetryLogger, IRegionProvider regionProvider, IAWSToolkitShellProvider toolkitShell)
        {
            _telemetryLogger = telemetryLogger;
            _toolkitShell = toolkitShell;
            SetupFactories();
            _credentialManager = new CredentialManager(_factoryMap);
            _awsConnectionManager = new AwsConnectionManager(Core.AwsConnectionManager.DefaultStsClientCreator,
                _credentialManager, _telemetryLogger, regionProvider);
            _persistConnectionSettings = new PersistConnectionSettings(_awsConnectionManager);
        }

        public void Initialize()
        {
           _awsConnectionManager.Initialize(_factoryMap.Values.ToList());
        }

        public void Dispose()
        {
            _persistConnectionSettings.Dispose();
            _awsConnectionManager?.Dispose();
            _credentialManager?.Dispose();
            foreach (var factory in _factoryMap)
            {
                factory.Value.Dispose();
            }
        }

        private void SetupFactories()
        {
            _factoryMap.Add(SDKCredentialProviderFactory.SdkProfileFactoryId, new SDKCredentialProviderFactory(_toolkitShell));
            _factoryMap.Add(SharedCredentialProviderFactory.SharedProfileFactoryId,
                new SharedCredentialProviderFactory(_toolkitShell));
            foreach (var factory in _factoryMap)
            {
                Setup(factory.Value);
            }
        }

        private void Setup(ICredentialProviderFactory factory)
        {
            // Track the credentials deltas to know how many valid credentials have been loaded
            int loadedCredentialsCount = 0;
            factory.CredentialsChanged += (sender, args) =>
            {
                var credentialsCount =
                    Interlocked.Add(ref loadedCredentialsCount, args.Added.Count - args.Removed.Count);
                _telemetryLogger.RecordAwsLoadCredentials(new AwsLoadCredentials()
                {
                    CredentialSourceId = factory.Id, Value = credentialsCount,
                });
            };

            factory.Initialize();
        }
    }
}
