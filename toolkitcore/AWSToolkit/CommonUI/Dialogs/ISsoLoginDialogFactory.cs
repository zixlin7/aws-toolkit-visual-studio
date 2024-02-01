using Amazon.Runtime;
using Amazon.Runtime.Credentials.Internal;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    /// <summary>
    /// Factory responsible for create SSO login dialogs (for IAM Credentials and bearer token auths)
    /// </summary>
    public interface ISsoLoginDialogFactory
    {
        ISsoLoginDialog CreateSsoIdcLoginDialog(SSOAWSCredentials ssoCredentials);

        ISsoLoginDialog CreateSsoBuilderIdLoginDialog(ISSOTokenManager ssoTokenManager,
            SSOTokenManagerGetTokenOptions tokenOptions);
    }
}
