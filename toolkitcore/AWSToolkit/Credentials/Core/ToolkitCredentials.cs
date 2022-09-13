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

        public ToolkitCredentials(ICredentialIdentifier credentialIdentifier, AWSCredentials awsCredentials)
        {
            CredentialIdentifier =
                credentialIdentifier ?? throw new ArgumentNullException(nameof(credentialIdentifier));
            _awsCredentials = awsCredentials;
        }

        public bool Supports(AwsConnectionType connectionType)
        {
            if (connectionType == AwsConnectionType.AwsCredentials)
            {
                return _awsCredentials != null;
            }

            if (connectionType == AwsConnectionType.AwsToken)
            {
                // todo : add token support
                return false;
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

        // todo : Add Get Token function when adding token support
    }
}
