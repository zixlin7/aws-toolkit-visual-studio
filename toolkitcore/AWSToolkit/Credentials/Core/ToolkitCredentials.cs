using System;

using Amazon.Runtime;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Contains one or more ways that can be used to connect to AWS
    /// </summary>
    public sealed class ToolkitCredentials
    {
        public ICredentialIdentifier CredentialIdentifier { get; }
        private readonly AWSCredentials _awsCredentials;
        private readonly IAWSTokenProvider _tokenProvider;

        public ToolkitCredentials(ICredentialIdentifier credentialIdentifier, AWSCredentials awsCredentials)
            : this(credentialIdentifier, awsCredentials, null)
        {
        }

        public ToolkitCredentials(ICredentialIdentifier credentialIdentifier, IAWSTokenProvider tokenProvider)
            : this(credentialIdentifier, null, tokenProvider)
        {
        }

        public ToolkitCredentials(ICredentialIdentifier credentialIdentifier, AWSCredentials awsCredentials, IAWSTokenProvider tokenProvider)
        {
            CredentialIdentifier =
                credentialIdentifier ?? throw new ArgumentNullException(nameof(credentialIdentifier));
            _awsCredentials = awsCredentials;
            _tokenProvider = tokenProvider;
        }

        public bool Supports(AwsConnectionType connectionType)
        {
            if (connectionType == AwsConnectionType.AwsCredentials)
            {
                return _awsCredentials != null;
            }

            if (connectionType == AwsConnectionType.AwsToken)
            {
                return _tokenProvider != null;
            }

            return false;
        }

        /// <summary>
        /// Callers are expected to know that their object works with AWSCredentials
        /// prior to using this method. Check with <see cref="Supports"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public AWSCredentials GetAwsCredentials()
        {
            if (_awsCredentials == null)
            {
                throw new InvalidOperationException("No AWSCredentials available");
            }

            return _awsCredentials;
        }

        /// <summary>
        /// Callers are expected to know that their object works with IAWSTokenProvider
        /// prior to using this method. Check with <see cref="Supports"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public IAWSTokenProvider GetTokenProvider()
        {
            if (_tokenProvider == null)
            {
                throw new InvalidOperationException("No IAWSTokenProvider available");
            }

            return _tokenProvider;
        }
    }
}
