using System;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Settings;
using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public class SDKCredentialProviderFactory : ProfileCredentialProviderFactory
    {
        public const string SdkProfileFactoryId = "SdkProfileCredentialProviderFactory";
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(SDKCredentialProviderFactory));
        private const double FileChangeDebounceInterval = 300;

        private readonly DebounceDispatcher _credentialsChangedDispatcher;
        private bool _disposed = false;
        private SettingsWatcher _watcher;
        public override string Id => SdkProfileFactoryId;

        private SDKCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell)
            : this(new ProfileHolder(),
                new SDKCredentialFileReader(),
                new SDKCredentialFileWriter(),
                toolkitShell)
        {
            _credentialsChangedDispatcher = new DebounceDispatcher();
        }

        public SDKCredentialProviderFactory(IProfileHolder holder, ICredentialFileReader fileReader,
            ICredentialFileWriter fileWriter, IAWSToolkitShellProvider toolkitShell)
            : base(holder, fileReader, fileWriter, toolkitShell)
        {
        }

        public override AWSCredentials CreateAwsCredential(ICredentialIdentifier identifierId, ToolkitRegion region)
        {
            var sdkIdentifierId = identifierId as SDKCredentialIdentifier ??
                                  throw new ArgumentException(
                                      $"SDKCredentialProviderFactory can only handle SDKCredentialIdentifiers, but got {identifierId.GetType()}");
            var sdkProfile = ProfileHolder.GetProfile(sdkIdentifierId.ProfileName) ??
                             throw new InvalidOperationException(
                                 $"Profile {sdkIdentifierId.ProfileName} looks to be removed.");

            return CreateAwsCredential(sdkProfile, region);
        }

        /// <summary>
        /// Instantiate and return true if an SDKCredentialProviderFactory can be created, else return false
        /// </summary>
        public static bool TryCreateFactory(IAWSToolkitShellProvider toolkitShell,
            out SDKCredentialProviderFactory factory)
        {
            var usableFactory = CanUseFactory();
            factory = usableFactory ? new SDKCredentialProviderFactory(toolkitShell) : null;
            return usableFactory;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (this._watcher != null)
                {
                    this._watcher.SettingsChanged -= HandleFileChangeEvent;
                    this._watcher.Dispose();
                }
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(CredentialProfile profile)
        {
            return new SDKCredentialIdentifier(profile.Name);
        }

        protected override void SetupProfileWatcher()
        {
            var persistenceManager = PersistenceManager.Instance as PersistenceManager;

            if (persistenceManager == null)
            {
                LOGGER.Error("Unable to access PersistenceManager - encrypted accounts may not loaded in the Explorer");
            }
            else
            {
                this._watcher =
                    persistenceManager.Watch(ToolkitSettingsConstants.RegisteredProfiles);
                this._watcher.SettingsChanged += DebounceAndHandleFileChange;
            }
        }

        private void DebounceAndHandleFileChange(object sender, EventArgs e)
        {
            _credentialsChangedDispatcher.Debounce(FileChangeDebounceInterval, _ => { HandleFileChangeEvent(sender, e); });
        }

        /// <summary>
        /// Checks if encrypted store is available or not
        /// If not, the factory cannot be used for credential resolution
        /// </summary>
        private static bool CanUseFactory()
        {
            return Runtime.Internal.Settings.UserCrypto.IsUserCryptAvailable;
        }
    }
}
