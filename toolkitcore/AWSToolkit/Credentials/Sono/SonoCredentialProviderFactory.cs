using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Credentials.Sono
{
    /// <summary>
    /// Responsible for producing a hard-coded credentials entity capable of getting a Token from Sono
    /// </summary>
    public class SonoCredentialProviderFactory : ICredentialProviderFactory
    {
        public const string FactoryId = "AwsBuilderId";

        public event EventHandler<CredentialChangeEventArgs> CredentialsChanged;

        public string Id => FactoryId;

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly SonoProfileProcessor _profileProcessor = new SonoProfileProcessor();
        private readonly string _tokenCacheFolder;

        public SonoCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell) : this(toolkitShell, null) { }

        /// <summary>
        /// Overload for testing purposes
        /// </summary>
        /// <param name="toolkitShell"></param>
        /// <param name="tokenCacheFolder">Location of the SSO Token cache. Set to null to use the default folder.</param>
        public SonoCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell, string tokenCacheFolder)
        {
            _toolkitShell = toolkitShell;
            _tokenCacheFolder = tokenCacheFolder;
        }

        public void Initialize()
        {
            // Populate settings manager with the fixed AWS Builder ID configurations that will back this factory

            var defaultCredentialId = new SonoCredentialIdentifier("default");
            _profileProcessor.CreateProfile(defaultCredentialId,
                new ProfileProperties()
                {
                    Name = defaultCredentialId.ProfileName,
                    // Assigning SsoSession is arbitrary, but helps resolve CredentialType
                    SsoSession = $"{FactoryId}-{defaultCredentialId.ProfileName}",
                    SsoStartUrl = SonoProperties.StartUrl,
                    SsoRegion = SonoProperties.DefaultTokenProviderRegion.SystemName,
                });
        }

        public List<ICredentialIdentifier> GetCredentialIdentifiers() =>
            _profileProcessor.GetCredentialIdentifiers().ToList();

        public ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            if (credentialIdentifier.FactoryId != FactoryId)
            {
                throw new ArgumentException(
                    $"Unexpected credential Id ({credentialIdentifier.Id}), expected type: {FactoryId}");
            }

            var profileProperties = _profileProcessor.GetProfileProperties(credentialIdentifier);
            if (profileProperties == null)
            {
                throw new NotSupportedException($"Unsupported AWS Builder ID based credential Id: {credentialIdentifier.Id}");
            }

            var tokenProvider = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(credentialIdentifier)
                .WithToolkitShell(_toolkitShell)
                .WithStartUrl(profileProperties.SsoStartUrl)
                .WithTokenProviderRegion(RegionEndpoint.GetBySystemName(profileProperties.SsoRegion))
                .Build();

            return new ToolkitCredentials(credentialIdentifier, tokenProvider);
        }

        public ICredentialProfileProcessor GetCredentialProfileProcessor() => _profileProcessor;

        public bool IsLoginRequired(ICredentialIdentifier id) => true;

        public bool Supports(ICredentialIdentifier credentialIdentifier, AwsConnectionType connectionType)
        {
            if (connectionType != AwsConnectionType.AwsToken) { return false; }

            return _profileProcessor.GetProfileProperties(credentialIdentifier) != null;
        }

        public virtual void Invalidate(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = _profileProcessor.GetProfileProperties(credentialIdentifier);
            if (profileProperties == null)
            {
                throw new NotSupportedException($"Unsupported AWS Builder ID based credential Id: {credentialIdentifier.Id}");
            }

            TokenCache.RemoveCacheFile(profileProperties.SsoStartUrl, SonoProperties.DefaultSessionName, _tokenCacheFolder);
        }

        public void Dispose()
        {
        }
    }
}
