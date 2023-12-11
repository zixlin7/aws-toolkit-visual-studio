using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Encapsulates the task of getting CodeWhisperer SSO Tokens.
    /// This allows us to stub components without exercising SSO tokens at the AWS SDK level.
    /// </summary>
    public interface ICodeWhispererSsoTokenProvider
    {
        /// <summary>
        /// Attempts to refresh the SSO token for <see cref="ConnectionProperties.CredentialIdentifier"/> without
        /// prompting the user for an SSO login flow.
        /// </summary>
        bool TrySilentGetSsoToken(ConnectionProperties connectionProperties, out AWSToken token);

        /// <summary>
        /// Obtain an SSO Token from the credentials system, taking the user through the SSO login flow if needed.
        /// </summary>
        TaskStatus TryGetSsoToken(ConnectionProperties connectionProperties, out AWSToken token);
    }
}
