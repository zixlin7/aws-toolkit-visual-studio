using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials
{
    public class FakeCodeWhispererSsoTokenProvider : ICodeWhispererSsoTokenProvider
    {
        /// <summary>
        /// Setting Token to null will simulate the "try-get" methods failing
        /// </summary>
        public AWSToken Token;

        /// <summary>
        /// Setting CanGetTokenSilently to false causes TrySilentGetSsoToken to return false,
        /// which simulates that the user would need to perform an sso login in order to get a token.
        /// </summary>
        public bool CanGetTokenSilently = false;

        public bool TrySilentGetSsoToken(ICredentialIdentifier credentialId, ToolkitRegion ssoRegion, out AWSToken token)
        {
            token = null;
            return CanGetTokenSilently && TryGetSsoToken(credentialId, ssoRegion, out token) == TaskStatus.Success;
        }

        public TaskStatus TryGetSsoToken(ICredentialIdentifier credentialId, ToolkitRegion ssoRegion, out AWSToken token)
        {
            token = null;

            if (Token == null)
            {
                return TaskStatus.Fail;
            }

            token = new AWSToken() { Token = Token.Token, ExpiresAt = Token.ExpiresAt, };
            return TaskStatus.Success;
        }
    }
}
