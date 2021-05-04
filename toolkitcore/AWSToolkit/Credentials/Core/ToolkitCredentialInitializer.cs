using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;

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
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ToolkitCredentialInitializer));

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
            try
            {
                AddSDKCredentialsFactory();
                AddSharedCredentialsFactory();
                foreach (var factory in _factoryMap)
                {
                    Setup(factory.Value);
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Unable to set up credentials properly, the Toolkit may not have access to all possible Credentials", ex);
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

        /// <summary>
        /// Add <see cref="SDKCredentialProviderFactory"/> to the factory map if it can be successfully created,
        /// else log the appropriate error message
        /// </summary>
        private void AddSDKCredentialsFactory()
        {
            try
            {
                if (SDKCredentialProviderFactory.TryCreateFactory(_toolkitShell,
                    out var factory))
                {
                    _factoryMap.Add(SDKCredentialProviderFactory.SdkProfileFactoryId, factory);
                }
                else
                {
                    LOGGER.Error(
                        "SDK Credentials Factory cannot be initialized: The encrypted store is not available. This may be due to use of a non - Windows operating system or Windows Nano Server, or the current user account may not have its profile loaded.");
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error while setting up SDK credentials for the toolkit.", ex);
            }
        }

        /// <summary>
        /// Add <see cref="SharedCredentialProviderFactory"/> to the factory map if it can be successfully created,
        /// else log the appropriate error message
        /// </summary>
        private void AddSharedCredentialsFactory()
        {
            try
            {
                if (SharedCredentialProviderFactory.TryCreateFactory(_toolkitShell,
                    out var factory))
                {
                    _factoryMap.Add(SharedCredentialProviderFactory.SharedProfileFactoryId, factory);
                }
                else
                {
                    LOGGER.Error(
                        "Shared Credentials Factory cannot be initialized.");
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error while setting up Shared credentials for the toolkit.", ex);
            }
        }

    }
}
