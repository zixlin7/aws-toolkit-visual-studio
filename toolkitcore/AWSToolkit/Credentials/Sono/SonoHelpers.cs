using System;

using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Sono
{
    /// <summary>
    /// Utility functions to produce portions of the Sono TokenProvider
    /// </summary>
    public static class SonoHelpers
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SonoHelpers));

        /// <summary>
        /// Produces an object that is configured to connect to Sono, and allows the Toolkit to
        /// prompt to start the SSO login flow.
        /// </summary>
        public static ToolkitSsoTokenManagerOptions CreateSonoTokenManagerOptions(Action<SsoVerificationArguments> ssoCallback, string[] scopes)
        {
            return new ToolkitSsoTokenManagerOptions(SonoProperties.ClientName, SonoProperties.ClientType, ssoCallback, scopes);
        }

        /// <summary>
        /// Produces an action that can be used in the SDK's SSO Token callback, which prompts the user
        /// to start the Sono (AWS Builder ID) login flow.
        ///
        /// This was created as an Action producer so that we could reference the credentials of interest within
        /// the prompt.
        /// </summary>
        public static Action<SsoVerificationArguments> CreateSsoCallback(ICredentialIdentifier credentialIdentifier,
            IAWSToolkitShellProvider toolkitShell, bool isBuilderId)
        {
            return (SsoVerificationArguments ssoVerification) =>
            {
                try
                {
                    // Prompt the user to start the AWS Builder ID Login flow
                    toolkitShell.ExecuteOnUIThread(() =>
                    {
                        var dialog = CreateDialog(toolkitShell, ssoVerification, isBuilderId);

                        if (!dialog.Show())
                        {
                            // Throw an exception to break out of the SDK IAWSTokenProvider.TryResolveToken call
                            throw new UserCanceledException("User declined to start login flow.");
                        }
                    });

                    var msgPrefix = $"{(isBuilderId ? "AWS Builder ID " : "")}Login flow started for Credentials: ";
                    toolkitShell.OutputToHostConsole($"{msgPrefix}{credentialIdentifier.DisplayName}", false);
                    _logger.Debug($"{msgPrefix}{credentialIdentifier.Id}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Error starting {(isBuilderId ? "AWS Builder ID " : "")}Login flow for {credentialIdentifier.Id}", ex);
                    throw;
                }
            };
        }

        private static ISsoLoginDialog CreateDialog(IAWSToolkitShellProvider toolkitShell, SsoVerificationArguments ssoVerification, bool isBuilderId)
        {
            var dialog = toolkitShell.GetDialogFactory().CreateSsoLoginDialog();
            dialog.IsBuilderId = isBuilderId;
            dialog.UserCode = ssoVerification.UserCode;
            dialog.LoginUri = ssoVerification.VerificationUri;
            return dialog;
        }
    }
}
