using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Handles the Connection state and operations for CodeWhisperer features.
    /// </summary>
    [Export(typeof(IConnection))]
    internal class Connection : IConnection, IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Connection));

        private readonly IToolkitContextProvider _toolkitContextProvider;

        private readonly IToolkitLspClient _codeWhispererLspClient;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;

        private ICredentialIdentifier _signedInCredentialIdentifier;
        private ToolkitRegion _signedInCredentialsSsoRegion;
        private DateTime? _tokenExpiresAt;
        private readonly IToolkitTimer _tokenRefreshTimer;

        [ImportingConstructor]
        public Connection(
            IToolkitContextProvider toolkitContextProvider,
            ICodeWhispererLspClient codeWhispererLspClient,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        : this(toolkitContextProvider, codeWhispererLspClient, taskFactoryProvider, new ToolkitTimer())
        {
        }

        /// <summary>
        /// Constructor overload for testing purposes
        /// </summary>
        internal Connection(
            IToolkitContextProvider toolkitContextProvider,
            ICodeWhispererLspClient codeWhispererLspClient,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider,
            IToolkitTimer tokenRefreshTimer)
        {
            _toolkitContextProvider = toolkitContextProvider;
            _codeWhispererLspClient = codeWhispererLspClient;
            _taskFactoryProvider = taskFactoryProvider;

            _tokenRefreshTimer = tokenRefreshTimer;
            _tokenRefreshTimer.AutoReset = false;
            _tokenRefreshTimer.Elapsed += OnTokenRefreshTimerElapsed;
        }

        private ConnectionStatus _status = ConnectionStatus.Disconnected;

        /// <summary>
        /// Gets the current status
        /// </summary>
        public ConnectionStatus Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnStatusChanged(new ConnectionStatusChangedEventArgs(_status));
            }
        }

        /// <summary>
        /// Event signaling that the status has changed
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        protected virtual void OnStatusChanged(ConnectionStatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Connects the user to CodeWhisperer.
        /// User may go through modal dialogs and login flows as a result.
        /// </summary>
        public async Task SignInAsync()
        {
            var credentialIdentifier = PromptUserForCredentialId();

            if (credentialIdentifier != null)
            {
                var displayName = credentialIdentifier?.DisplayName ?? "unknown";

                try
                {
                    var toolkitContext = _toolkitContextProvider.GetToolkitContext();
                    var profile = toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);
                    var region = toolkitContext.RegionProvider.GetRegion(profile.SsoRegion);
                    var toolkitCredentials = toolkitContext.CredentialManager.GetToolkitCredentials(credentialIdentifier, region);

                    if (!toolkitCredentials.GetTokenProvider().TryResolveToken(out var awsToken))
                    {
                        var msg = $"Cannot sign in to CodeWhisperer.  Unable to resolve bearer token {displayName}.";
                        NotifyErrorAndDisconnect(msg);
                        throw new InvalidOperationException(msg);
                    }

                    if (await UseAwsTokenAsync(awsToken, credentialIdentifier, region))
                    {
                        toolkitContext.ToolkitHost.OutputToHostConsole($"Signed in to CodeWhisperer with {displayName}.");
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"Failed to sign in to CodeWhisperer with {displayName}.";
                    NotifyErrorAndDisconnect(msg, ex);
                    throw new InvalidOperationException(msg, ex);
                }
            }
        }

        /// <summary>
        /// Prompts user for credential Id to use to sign-in to CodeWhisperer.
        /// </summary>
        /// <remarks>
        /// This function isolates the prompt and UI related code, so that we can stub it in testing.
        /// </remarks>
        /// <returns>User-selected credentialId, null if user cancelled.</returns>
        protected virtual ICredentialIdentifier PromptUserForCredentialId()
        {
            var viewModel = new CredentialSelectionDialogViewModel(_toolkitContextProvider);
            var dlg = new CredentialSelectionDialog
            {
                DataContext = viewModel
            };

            return dlg.ShowModal() == true ? viewModel.SelectedCredentialIdentifier : null;
        }

        /// <summary>
        /// Signs the user out of CodeWhisperer
        /// </summary>
        public async Task SignOutAsync()
        {
            var credentialId = _signedInCredentialIdentifier;
            var displayName = credentialId?.DisplayName ?? "<unknown>";

            try
            {
                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                if (await ClearAwsTokenAsync())
                {
                    _toolkitContextProvider.GetToolkitContext().CredentialManager.Invalidate(credentialId);
                    toolkitContext.ToolkitHost.OutputToHostConsole($"Signed out of CodeWhisperer with {displayName}.");
                }
            }
            catch (Exception ex)
            {
                var msg = $"Failed to sign out of CodeWhisperer with {displayName}.";
                NotifyError(msg, ex);
                throw new InvalidOperationException(msg, ex);
            }
        }

        private void NotifyErrorAndDisconnect(string message, Exception ex = null)
        {
            NotifyError(message, ex);
            Status = ConnectionStatus.Disconnected;
        }

        private void NotifyError(string message, Exception ex = null)
        {
            _logger.Error(message, ex);

            var toolkitContext = _toolkitContextProvider.GetToolkitContext();
            toolkitContext.ToolkitHost.OutputToHostConsole(message);
            toolkitContext.ToolkitHost.ShowError(message, ex?.Message ?? message);
        }

        private Task<bool> ClearAwsTokenAsync()
        {
            return UseAwsTokenAsync(null, null, null);
        }

        /// <summary>
        /// Updates the connection state (and adjusts the refresh timer) based on the given token.
        /// Signed in and refreshed tokens should be passed to this method.
        /// To "clear" a token (sign out), pass null for all parameters, or use <see cref="ClearAwsTokenAsync"/>.
        /// </summary>
        private async Task<bool> UseAwsTokenAsync(
            AWSToken token,
            ICredentialIdentifier credentialIdentifier,
            ToolkitRegion credentialsSsoRegion)
        {
            var credentialsProtocol = _codeWhispererLspClient.CreateToolkitLspCredentials();

            _tokenRefreshTimer.Stop();

            if (token == null)
            {
                // Indicates "sign out"
                credentialsProtocol.DeleteToken();

                _signedInCredentialIdentifier = null;
                _signedInCredentialsSsoRegion = null;
                _tokenExpiresAt = null;
                Status = ConnectionStatus.Disconnected;
            }
            else
            {
                // Indicates "sign in" or "token refresh"

                // TODO : Soon UpdateTokenAsync will return a response. Return true or false based on the response.
                await credentialsProtocol.UpdateTokenAsync(new BearerToken() { Token = token.Token });

                _signedInCredentialIdentifier = credentialIdentifier;
                _signedInCredentialsSsoRegion = credentialsSsoRegion;
                _tokenExpiresAt = token.ExpiresAt;
                Status = ConnectionStatus.Connected;

                // Try to refresh this token at some point in the future.
                if (token.ExpiresAt.HasValue)
                {
                    ResetTokenRefreshTimer(token.ExpiresAt.Value);
                }
            }

            return true;
        }

        private static class RefreshOffsets
        {
            public const int MinutesBeforeExpiration = 5;
            public const int MinutesFromNowDuringRefreshWindow = 1;
            public const int SecondsAfterExpiration = 1;
            public const int SecondsFromNowAfterExpiration = 3;
        }

        /// <summary>
        /// Sets up the system to attempt a refresh at some time in the future
        /// based on the given token expiration time.
        /// </summary>
        private void ResetTokenRefreshTimer(DateTime tokenExpiresAt)
        {
            tokenExpiresAt = tokenExpiresAt.ToUniversalTime();
            var now = DateTime.UtcNow;

            // The soonest we would try to refresh in relation to the token expiration
            // is five minutes before token expiration. If that has already passed, we
            // will try in one minute (this allows a refresh attempt to "cool off").
            var earliestRefreshAttempt = tokenExpiresAt.AddMinutes(-RefreshOffsets.MinutesBeforeExpiration);
            if (earliestRefreshAttempt < now)
            {
                earliestRefreshAttempt = now.AddMinutes(RefreshOffsets.MinutesFromNowDuringRefreshWindow);
            }

            // The latest we would try to refresh in relation to the token expiration
            // is one second after token expiration. If that has already passed, we
            // will try immediately (this will force us into an expired state).
            var latestRefreshAttempt = tokenExpiresAt.AddSeconds(RefreshOffsets.SecondsAfterExpiration);
            if (latestRefreshAttempt < now)
            {
                latestRefreshAttempt = now.AddSeconds(RefreshOffsets.SecondsFromNowAfterExpiration);
            }

            var nextInvokeCandidates = new DateTime[]
            {
                earliestRefreshAttempt,
                latestRefreshAttempt,
            };

            // set timer, based on nextInvokeAt
            var nextInvokeAt = nextInvokeCandidates
                .Where(x => x > DateTime.UtcNow)
                .Min();

            var interval = (nextInvokeAt - DateTime.UtcNow).TotalMilliseconds;

            _tokenRefreshTimer.Interval = interval;
            _tokenRefreshTimer.Start();
        }

        private void OnTokenRefreshTimerElapsed(object sender, ToolkitTimerElapsedEventArgs e)
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () => await OnTokenRefreshTimerElapsedAsync());
        }

        /// <summary>
        /// Attempts to refresh the token.
        /// If not successful, it will either try again shortly, or expire the connection state.
        /// </summary>
        private async Task OnTokenRefreshTimerElapsedAsync()
        {
            if (!TrySilentRefresh(out var refreshToken))
            {
                // Unable to refresh the token (or retrieve the current one)
                // Move the connection state to Expired, then disconnect.
                Status = ConnectionStatus.Expired; // event handlers react to this
                await ClearAwsTokenAsync();

                return;
            }

            // We either successfully refreshed the token, or AWS SDK gave us the same token back.
            if (refreshToken.ExpiresAt > _tokenExpiresAt)
            {
                // Token Refresh was successful
                await UseAwsTokenAsync(refreshToken, _signedInCredentialIdentifier, _signedInCredentialsSsoRegion);
            }
            else
            {
                // AWS SDK gave us the same token. Try again shortly.
                if (refreshToken.ExpiresAt.HasValue)
                {
                    ResetTokenRefreshTimer(refreshToken.ExpiresAt.Value);
                }
            }
        }

        /// <summary>
        /// Attempts to refresh the SSO token for the current signed-in credentialId without
        /// prompting the user for an SSO login flow.
        /// </summary>
        private bool TrySilentRefresh(out AWSToken refreshToken)
        {
            refreshToken = null;

            var toolkitContext = _toolkitContextProvider.GetToolkitContext();

            // Try to refresh the token in a way that fails if the user would need to be prompted for a SSO Login
            if (!_signedInCredentialIdentifier.HasValidToken(SonoCredentialProviderFactory.CodeWhispererSsoSession, toolkitContext.ToolkitHost))
            {
                return false;
            }

            // The token has been refreshed. We can now obtain it through the credentials engine
            // with confidence that the user will not get prompted to perform an SSO login.
            var toolkitCredentials = toolkitContext.CredentialManager.GetToolkitCredentials(
                _signedInCredentialIdentifier, _signedInCredentialsSsoRegion);

            return toolkitCredentials.GetTokenProvider().TryResolveToken(out refreshToken);
        }

        public void Dispose()
        {
            _tokenRefreshTimer.Elapsed -= OnTokenRefreshTimerElapsed;
            _tokenRefreshTimer?.Dispose();
        }
    }
}
