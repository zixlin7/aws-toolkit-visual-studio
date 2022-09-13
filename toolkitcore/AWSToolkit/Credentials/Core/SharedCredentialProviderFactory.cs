using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Core
{
    public class SharedCredentialProviderFactory : ProfileCredentialProviderFactory
    {
        public const string SharedProfileFactoryId = "SharedProfileCredentialProviderFactory";
        public override string Id => SharedProfileFactoryId;

        private readonly SharedCredentialsFile _credentialsFile;
        private bool _disposed = false;
        private ProfileWatcher _watcher;

        private SharedCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell, SharedCredentialsFile credentialsFile) : this(new ProfileHolder(),
            new SharedCredentialFileReader(credentialsFile), new SharedCredentialFileWriter(credentialsFile), toolkitShell)
        {
            _credentialsFile = credentialsFile;
        }

        public SharedCredentialProviderFactory(IProfileHolder holder, ICredentialFileReader fileReader,
            ICredentialFileWriter fileWriter, IAWSToolkitShellProvider toolkitShell)
            : base(holder, fileReader, fileWriter, toolkitShell)
        {
        }

        public override ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            var sharedIdentifierId = credentialIdentifier as SharedCredentialIdentifier ??
                                     throw new ArgumentException(
                                         $"SharedCredentialProviderFactory expected {nameof(SharedCredentialIdentifier)}, but received {credentialIdentifier.GetType()}");
            var sharedProfile = ProfileHolder.GetProfile(sharedIdentifierId.ProfileName) ??
                                throw new InvalidOperationException(
                                    $"Profile not found: {sharedIdentifierId.ProfileName}");

            var awsCredentials = CreateAwsCredential(sharedProfile, region);

            return new ToolkitCredentials(credentialIdentifier, awsCredentials);
        }

        /// <summary>
        /// Instantiate and return true if an SharedCredentialProviderFactory can be created, else return false
        /// </summary>
        public static bool TryCreateFactory(IAWSToolkitShellProvider toolkitShell, out SharedCredentialProviderFactory factory)
        {
            factory = new SharedCredentialProviderFactory(toolkitShell, new SharedCredentialsFile());
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_watcher != null)
                {
                    _watcher.Changed -= HandleFileChangeEvent;
                    _watcher.Dispose();
                }
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(CredentialProfile profile)
        {
            return new SharedCredentialIdentifier(profile.Name);
        }

        protected override AWSCredentials CreateSaml(CredentialProfile profile)
        {
            throw new InvalidOperationException($"Error creating credentials for {profile.Name}: SAML based profiles are not supported with a Shared Credentials File.");
        }

        protected override void SetupProfileWatcher()
        {
            var credentialFilePaths = GetCandidateCredentialFilePaths();
            _watcher = new ProfileWatcher(credentialFilePaths);
            _watcher.Changed += HandleFileChangeEvent;
        }

        private List<string> GetCandidateCredentialFilePaths()
        {
            var credentialPaths = new List<string> {SharedCredentialsFile.DefaultFilePath, AWSConfigs.AWSProfilesLocation,};
            credentialPaths.AddRange(GetCurrentSharedCredentialsPath());

            return credentialPaths
                .Select(NormalizePath)
                .Distinct()
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
        }

        /// <summary>
        /// Attempts to determine the path of the shared credentials file and config file
        /// </summary>
        /// <returns>The current path of the shared credentials file and config file, if it could be found and loaded successfully. Null otherwise.</returns>
        private List<string> GetCurrentSharedCredentialsPath()
        {
            try
            {
                var credentialsFile = new SharedCredentialsFile();
                var configFilePath = "";
                if (!string.IsNullOrWhiteSpace(credentialsFile.FilePath))
                {
                    configFilePath = Path.Combine(Path.GetDirectoryName(credentialsFile.FilePath), "config");
                }

                return new List<string> {credentialsFile.FilePath, configFilePath};
            }
            catch (Exception)
            {
                return new List<string> {null};
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(path)
                    .TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar
                    );
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
