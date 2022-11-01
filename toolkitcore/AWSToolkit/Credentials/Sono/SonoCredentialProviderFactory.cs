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
        public const string FactoryId = "AwsId";

        public event EventHandler<CredentialChangeEventArgs> CredentialsChanged;

        public string Id => FactoryId;

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly SonoProfileProcessor _profileProcessor = new SonoProfileProcessor();

        public SonoCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell)
        {
            _toolkitShell = toolkitShell;
        }

        public void Initialize()
        {
            // Populate settings manager with the fixed AWS ID configurations that will back this factory

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
                throw new NotSupportedException($"Unsupported AWS ID based credential Id: {credentialIdentifier.Id}");
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

        public void Dispose()
        {
        }
    }
}
