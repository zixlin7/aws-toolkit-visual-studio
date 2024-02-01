using System;

using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// This is the Toolkit Credentials wrapper around resolving SSO based credentials.
    /// </summary>
    public class AwsSsoCredentials : AWSCredentials
    {
        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly CredentialProfile _profile;
        private readonly SSOAWSCredentials _ssoCredentials;
        private readonly SSOAWSCredentials _silentSsoCredentials;

        public AwsSsoCredentials(CredentialProfile profile, IAWSToolkitShellProvider toolkitShell)
        {
            _profile = profile;
            _toolkitShell = toolkitShell;

            _silentSsoCredentials = CreateSsoCredentials();
            _ssoCredentials = CreateSsoCredentials();
        }

        public override ImmutableCredentials GetCredentials()
        {
            try
            {
                // attempt to get credentials without a user login
                if (TryGetSilentCredentials(out var credentials))
                {
                    return credentials;
                }
                // get credentials by prompting for a user login
                _toolkitShell.ExecuteOnUIThread(() =>
                {
                    using (var dialog = CreateLoginDialog())
                    {
                        var result = dialog.DoLoginFlow();
                        result.ThrowIfUnsuccessful();
                        credentials = dialog.Credentials;
                    }
                });
                return credentials;
            }
            catch (Exception e)
            {
                _toolkitShell.OutputToHostConsole(
                    $"Login failed for AWS IAM Identity Center (SSO) based profile {_profile.Name}: {e.Message}",
                    false);
                throw;
            }
        }

        private SSOAWSCredentials CreateSsoCredentials()
        {
            return new SSOAWSCredentials(
                _profile.Options.SsoAccountId,
                _profile.Options.SsoRegion,
                _profile.Options.SsoRoleName,
                _profile.Options.SsoStartUrl,
                new SSOAWSCredentialsOptions()
                {
                    ClientName = SonoProperties.ClientName,
                    // Must match SSOTokenProvider session or will prompt to login through browser again
                    SessionName = _profile.Options.SsoSession,
                    SsoVerificationCallback = obj => throw new Exception("AWS IAM IDC profile requires a login flow")
                });
        }

        /// <summary>
        /// Attempts to retrieve/resolve credentials without performing a user login
        /// </summary>
        private bool TryGetSilentCredentials(out ImmutableCredentials credentials)
        {
            credentials = null;
            try
            {
                credentials = _silentSsoCredentials.GetCredentials();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private ISsoLoginDialog CreateLoginDialog()
        {
            var ssoDialogFactory = _toolkitShell.GetDialogFactory().CreateSsoLoginDialogFactory(_profile.Name);
            return ssoDialogFactory.CreateSsoIdcLoginDialog(_ssoCredentials);
        }
    }
}
