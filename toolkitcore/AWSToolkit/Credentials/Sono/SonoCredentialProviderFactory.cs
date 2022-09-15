using System;
using System.Collections.Generic;

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
        public const string FactoryId = "Sono";
        private static readonly SonoCredentialIdentifier CredentialId = new SonoCredentialIdentifier("sono");

        public event EventHandler<CredentialChangeEventArgs> CredentialsChanged;

        public string Id => FactoryId;

        private readonly IAWSToolkitShellProvider _toolkitShell;

        public SonoCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell)
        {
            _toolkitShell = toolkitShell;
        }

        public void Initialize()
        {
        }

        public List<ICredentialIdentifier> GetCredentialIdentifiers() =>
            new List<ICredentialIdentifier>() { CredentialId };

        public ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            if (credentialIdentifier.FactoryId != FactoryId)
            {
                throw new ArgumentException(
                    $"Unexpected credential Id ({credentialIdentifier.Id}), expected type: {FactoryId}");
            }

            if (credentialIdentifier.Id != CredentialId.Id)
            {
                throw new NotSupportedException($"Unsupported Sono credential Id: {credentialIdentifier.Id}");
            }

            var tokenProvider = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(credentialIdentifier)
                .WithToolkitShell(_toolkitShell)
                .Build();

            return new ToolkitCredentials(credentialIdentifier, tokenProvider);
        }

        public ICredentialProfileProcessor GetCredentialProfileProcessor() => null;

        public bool IsLoginRequired(ICredentialIdentifier id) => true;

        public bool Supports(ICredentialIdentifier credentialIdentifier, AwsConnectionType connectionType)
        {
            return credentialIdentifier?.Id == CredentialId.Id && connectionType == AwsConnectionType.AwsToken;
        }

        public void Dispose()
        {
        }
    }
}
