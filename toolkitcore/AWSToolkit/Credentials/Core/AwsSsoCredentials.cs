using System;
using System.Diagnostics;
using System.Windows;

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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AwsSsoCredentials));

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
                _toolkitShell.OutputToHostConsole($"Login failed for AWS SSO based profile {_profile.Name}: {e.Message}", false);
                throw;
            }
        }

        private void StartSsoLogin(SsoVerificationArguments ssoVerification)
        {
            try
            {
                // Prompt the user to start the SSO Login flow
                var title = "AWS SSO Login Required";
                var message =
                    $"AWS Toolkit would like to start the SSO Login process for Credentials Profile {_profile.Name} by visiting the following URL and using the following code:{Environment.NewLine}{Environment.NewLine}URL: {ssoVerification.VerificationUri}{Environment.NewLine}Code: {ssoVerification.UserCode}";
                if (!_toolkitShell.Confirm(title, message, MessageBoxButton.OKCancel))
                {
                    // Throw an exception to break out of the SDK AWSCredentials.GetCredentials call
                    throw new UserCanceledException("User declined to start SSO Login Flow");
                }

                _toolkitShell.OutputToHostConsole($"SSO Login flow started for Credentials: {_profile.Name}", false);
                Logger.Debug($"SSO Login flow started for Credentials: {_profile.Name}");

                Process.Start(ssoVerification.VerificationUriComplete);
            }
            catch (Exception e) {
                Logger.Error($"Error starting SSO Login flow for {_profile.Name}", e);
                throw;
            }
        }
    }
}
