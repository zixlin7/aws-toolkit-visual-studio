using System;

using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// This is the Toolkit Credentials wrapper around resolving SSO based credentials.
    /// </summary>
    public class AwsSsoCredentials : AWSCredentials
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(AwsSsoCredentials));

        private readonly IAWSToolkitShellProvider _toolkitShell;
        private readonly CredentialProfile _profile;
        private readonly SSOAWSCredentials _ssoCredentials;

        public AwsSsoCredentials(CredentialProfile profile, IAWSToolkitShellProvider toolkitShell)
        {
            var ssoCredentials = new SSOAWSCredentials(profile.Options.SsoAccountId, profile.Options.SsoRegion, profile.Options.SsoRoleName, profile.Options.SsoStartUrl);
            ssoCredentials.Options.ClientName = "aws-toolkit-visual-studio";
            ssoCredentials.Options.SsoVerificationCallback = StartSsoLogin;

            _profile = profile;
            _ssoCredentials = ssoCredentials;
            _toolkitShell = toolkitShell;
        }

        public override ImmutableCredentials GetCredentials()
        {
            try
            {
                return _ssoCredentials.GetCredentials();
            }
            catch (Exception e)
            {
                _toolkitShell.OutputToHostConsole($"Login failed for AWS IAM Identity Center (SSO) based profile {_profile.Name}: {e.Message}", false);
                throw;
            }
        }

        private void StartSsoLogin(SsoVerificationArguments ssoVerification)
        {
            try
            {
                // Prompt the user to start the SSO Login flow
                _toolkitShell.ExecuteOnUIThread(() =>
                {
                    var dialog = CreateDialog(ssoVerification);
                    if (!dialog.Show())
                    {
                        // Throw an exception to break out of the SDK  AWSCredentials.GetCredentials call
                        throw new UserCanceledException("User declined to start AWS IAM Identity Center (SSO) Login Flow");
                    }
                });

                _toolkitShell.OutputToHostConsole($"AWS IAM Identity Center (SSO) Login flow started for Credentials: {_profile.Name}", false);
                _logger.Debug($"AWS IAM Identity Center (SSO) Login flow started for Credentials: {_profile.Name}");

            }
            catch (Exception e)
            {
                _logger.Error($"Error starting AWS IAM Identity Center (SSO) Login flow for {_profile.Name}", e);
                throw;
            }
        }

        private ISsoLoginDialog CreateDialog(SsoVerificationArguments ssoVerification)
        {
            var dialog = _toolkitShell.GetDialogFactory().CreateSsoLoginDialog();
            dialog.IsBuilderId = false;
            dialog.UserCode = ssoVerification.UserCode;
            dialog.LoginUri = ssoVerification.VerificationUri;
            dialog.CredentialName = _profile.Name;
            return dialog;
        }
    }
}
