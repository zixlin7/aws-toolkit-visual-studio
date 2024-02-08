using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    /// <summary>
    /// Factory responsible for create SSO login dialogs (for IAM Credentials and bearer token auths)
    /// </summary>
    public interface ISsoLoginDialogFactory
    {
        ISsoLoginDialog CreateSsoCredentialsProviderLoginDialog(SSOAWSCredentials ssoCredentials);

        ISsoLoginDialog CreateSsoTokenProviderLoginDialog(ISSOTokenManager ssoTokenManager,
            SSOTokenManagerGetTokenOptions tokenOptions, bool isBuilderId);
    }
}
