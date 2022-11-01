using System;
using System.Diagnostics;
using System.Windows;

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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SonoHelpers));

        /// <summary>
        /// Produces an object that is configured to connect to Sono, and allows the Toolkit to
        /// prompt to start the SSO login flow.
        /// </summary>
        public static ToolkitSsoTokenManagerOptions CreateSonoTokenManagerOptions(
            ICredentialIdentifier credentialIdentifier,
            IAWSToolkitShellProvider toolkitShell)
        {
            return new ToolkitSsoTokenManagerOptions(SonoProperties.ClientName, SonoProperties.ClientType,
                CreateSsoCallback(credentialIdentifier, toolkitShell), SonoProperties.Scopes);
        }

        /// <summary>
        /// Produces an action that can be used in the SDK's SSO Token callback, which prompts the user
        /// to start the Sono (AWS ID) login flow.
        ///
        /// This was created as an Action producer so that we could reference the credentials of interest within
        /// the prompt.
        /// </summary>
        public static Action<SsoVerificationArguments> CreateSsoCallback(ICredentialIdentifier credentialIdentifier,
            IAWSToolkitShellProvider toolkitShell)
        {
            return (SsoVerificationArguments ssoVerification) =>
            {
                try
                {
                    // Prompt the user to start the SSO Login flow
                    var title = "Login with AWS ID Required";
                    var message =
                        $"AWS Toolkit will start the AWS ID Login process for {credentialIdentifier.DisplayName}.{Environment.NewLine}{Environment.NewLine}"
                        + $"Clicking OK will take you to the following URL in order to sign in and allow access to the Toolkit: {Environment.NewLine}{ssoVerification.VerificationUriComplete}";
                    if (!toolkitShell.Confirm(title, message, MessageBoxButton.OKCancel))
                    {
                        // Throw an exception to break out of the SDK IAWSTokenProvider.TryResolveToken call
                        throw new UserCanceledException("User declined to start AWS ID Login Flow");
                    }

                    toolkitShell.OutputToHostConsole(
                        $"AWS ID Login flow started for Credentials: {credentialIdentifier.DisplayName}", false);
                    Logger.Debug($"AWS ID Login flow started for Credentials: {credentialIdentifier.Id}");

                    Process.Start(ssoVerification.VerificationUriComplete);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error starting AWS ID Login flow for {credentialIdentifier.Id}", e);
                    throw;
                }
            };
        }
    }
}
