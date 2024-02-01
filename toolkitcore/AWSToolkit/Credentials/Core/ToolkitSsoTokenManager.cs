using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.CommonUI.Notifications;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime.Credentials.Internal;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Wraps a provided SSO Token Manager in order to configure the Token Request options as needed.
    /// </summary>
    public sealed class ToolkitSsoTokenManager : ISSOTokenManager
    {
        private readonly ISSOTokenManager _ssoTokenManager;
        private readonly ToolkitSsoTokenManagerOptions _toolkitTokenManagerOptions;
        private readonly IAWSToolkitShellProvider _toolkitShell;

        public ToolkitSsoTokenManager(ISSOTokenManager ssoTokenManager, ToolkitSsoTokenManagerOptions toolkitTokenManagerOptions, IAWSToolkitShellProvider toolkitShell)
        {
            _ssoTokenManager = ssoTokenManager;
            _toolkitTokenManagerOptions = toolkitTokenManagerOptions;
            _toolkitShell = toolkitShell;
        }

        public SsoToken GetToken(SSOTokenManagerGetTokenOptions options)
        {
            SetupOptions(options);

            // if a callback handler has been provided use that, else use the default callback flow to retrieve the token
            if (_toolkitTokenManagerOptions.SsoVerificationCallback != null)
            {
                return _ssoTokenManager.GetToken(options);
            }

            // first attempt to retrieve token without a user login and if it fails prompt user to login
            var token = TrySilentGetToken(options);
            return token ?? GetTokenWithLogin(options);
        }

        public async Task<SsoToken> GetTokenAsync(SSOTokenManagerGetTokenOptions options, CancellationToken cancellationToken = default)
        {
            SetupOptions(options);

            // if a callback handler has been provided use that, else use the default callback flow to retrieve the token
            if (_toolkitTokenManagerOptions.SsoVerificationCallback != null)
            {
                return await _ssoTokenManager.GetTokenAsync(options, cancellationToken);
            }

            // first attempt to retrieve token without a user login and if it fails prompt user to login
            var token = await TrySilentGetTokenAsync(options, cancellationToken);
            return token ?? GetTokenWithLogin(options, cancellationToken);
        }

        private SsoToken GetTokenWithLogin(SSOTokenManagerGetTokenOptions options, CancellationToken cancellationToken = default)
        {
            SsoToken token = null;
            try
            {
                _toolkitShell.ExecuteOnUIThread(() =>
                {
                    using (var dialog = CreateLoginDialog(options, cancellationToken))
                    {
                        var result = dialog.DoLoginFlow();
                        // Throw an exception to break out of the SDK IAWSTokenProvider.TryResolveToken call
                        result.ThrowIfUnsuccessful();
                        token = dialog.SsoToken;
                    }
                });
                return token;
            }
            catch (Exception ex)
            {
                _toolkitShell.OutputToHostConsole(
                    $"Login failed for AWS Builder ID {_toolkitTokenManagerOptions.CredentialName}: {ex.Message}");
                throw;
            }
        }

        private ISsoLoginDialog CreateLoginDialog(SSOTokenManagerGetTokenOptions options, CancellationToken cancellationToken)
        {
            var ssoDialogFactory = _toolkitShell.GetDialogFactory()
                .CreateSsoLoginDialogFactory(_toolkitTokenManagerOptions.CredentialName, cancellationToken);
            return ssoDialogFactory.CreateSsoBuilderIdLoginDialog(_ssoTokenManager, options);
        }

        /// <summary>
        /// Attempts to retrieve/resolve token silently (without performing a user login)
        /// </summary>
        private SsoToken TrySilentGetToken(SSOTokenManagerGetTokenOptions options)
        {
            try
            {
                // Setup token manager options without actual sso callback required for user login
                options.SsoVerificationCallback = args
                    => throw new Exception("AWS Builder ID requires a login flow");
                return _ssoTokenManager.GetToken(options);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to retrieve/resolve token silently (without performing a user login)
        /// </summary>
        private async Task<SsoToken> TrySilentGetTokenAsync(SSOTokenManagerGetTokenOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                // Setup token manager options without actual sso callback required for user login
                options.SsoVerificationCallback = args
                    => throw new Exception("AWS Builder ID requires a login flow");
                return await _ssoTokenManager.GetTokenAsync(options, cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }
       
        private void SetupOptions(SSOTokenManagerGetTokenOptions options)
        {
            options.SsoVerificationCallback = _toolkitTokenManagerOptions.SsoVerificationCallback;
            options.SupportsGettingNewToken = true;
            options.ClientName = _toolkitTokenManagerOptions.ClientName;
            options.ClientType = _toolkitTokenManagerOptions.ClientType;

            if (options.Scopes == null)
            {
                options.Scopes = new List<string>();
            }

            options.Scopes.AddAll(_toolkitTokenManagerOptions.Scopes.Except(options.Scopes));
        }
    }
}
