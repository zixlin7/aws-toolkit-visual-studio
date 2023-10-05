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
        public const string CodeCatalystProfileName = "codecatalyst";

        public const string CodeWhispererProfileName = "codewhisperer";

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
        internal SonoCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell, string tokenCacheFolder)
        {
            _toolkitShell = toolkitShell;
            _tokenCacheFolder = tokenCacheFolder;
        }

        public void Initialize()
        {
            CreateProfile(CodeCatalystProfileName, SonoProperties.CodeCatalystScopes);
            CreateProfile(CodeWhispererProfileName, SonoProperties.CodeWhispererScopes);
        }

        private void CreateProfile(string profileName, string[] ssoRegistrationScopes)
        { 
            // Populate settings manager with the fixed AWS Builder ID configurations that will back this factory
            var credId = new SonoCredentialIdentifier(profileName);
            _profileProcessor.CreateProfile(credId,
                new ProfileProperties()
                {
                    Name = credId.ProfileName,
                    SsoRegistrationScopes = ssoRegistrationScopes,
                    SsoSession = $"{FactoryId}-{credId.ProfileName}", // SsoSession helps resolve CredentialType
                    SsoStartUrl = SonoProperties.StartUrl,
                    SsoRegion = SonoProperties.DefaultTokenProviderRegion.SystemName,
                });
        }

        public List<ICredentialIdentifier> GetCredentialIdentifiers()
        {
            return _profileProcessor.GetCredentialIdentifiers().ToList();
        }

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
                .WithScopes(profileProperties.SsoRegistrationScopes)
                .WithStartUrl(profileProperties.SsoStartUrl)
                .WithTokenProviderRegion(RegionEndpoint.GetBySystemName(profileProperties.SsoRegion))
                .WithToolkitShell(_toolkitShell)
                .Build();

            return new ToolkitCredentials(credentialIdentifier, tokenProvider);
        }

        public ICredentialProfileProcessor GetCredentialProfileProcessor()
        {
            return _profileProcessor;
        }

        public bool IsLoginRequired(ICredentialIdentifier id)
        {
            return true;
        }

        public bool Supports(ICredentialIdentifier credentialIdentifier, AwsConnectionType connectionType)
        {
            return
                connectionType == AwsConnectionType.AwsToken
                && _profileProcessor.GetProfileProperties(credentialIdentifier) != null;
        }

        public virtual void Invalidate(ICredentialIdentifier credentialIdentifier)
        {
            var profileProperties = _profileProcessor.GetProfileProperties(credentialIdentifier);
            if (profileProperties == null)
            {
                throw new NotSupportedException($"Unsupported AWS Builder ID based credential Id: {credentialIdentifier.Id}");
            }

            TokenCache.RemoveCacheFile(profileProperties.SsoStartUrl, credentialIdentifier.ToDefaultSessionName(), _tokenCacheFolder);
        }

        public void Dispose()
        {
        }
    }
}
