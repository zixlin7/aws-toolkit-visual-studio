using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
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

        private readonly ICodeWhispererSettingsRepository _settingsRepository;
        private readonly IToolkitLspClient _codeWhispererLspClient;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly ICodeWhispererSsoTokenProvider _tokenProvider;

        private ConnectionProperties _signedInConnectionProperties;
        private DateTime? _tokenExpiresAt;
        private readonly IToolkitTimer _tokenRefreshTimer;

        [ImportingConstructor]
        public Connection(
            IToolkitContextProvider toolkitContextProvider,
            ICodeWhispererSettingsRepository settingsRepository,
            ICodeWhispererLspClient codeWhispererLspClient,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        : this(
            toolkitContextProvider, settingsRepository, codeWhispererLspClient,
            new CodeWhispererSsoTokenProvider(toolkitContextProvider),
            taskFactoryProvider, new ToolkitTimer())
        {
        }

        /// <summary>
        /// Constructor overload for testing purposes
        /// </summary>
        internal Connection(
            IToolkitContextProvider toolkitContextProvider,
            ICodeWhispererSettingsRepository settingsRepository,
            ICodeWhispererLspClient codeWhispererLspClient,
            ICodeWhispererSsoTokenProvider tokenProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider,
            IToolkitTimer tokenRefreshTimer)
        {
            _toolkitContextProvider = toolkitContextProvider;
            _settingsRepository = settingsRepository;
            _codeWhispererLspClient = codeWhispererLspClient;
            _tokenProvider = tokenProvider;
            _taskFactoryProvider = taskFactoryProvider;

            _tokenRefreshTimer = tokenRefreshTimer;
            _tokenRefreshTimer.AutoReset = false;
            _tokenRefreshTimer.Elapsed += OnTokenRefreshTimerElapsed;

            _codeWhispererLspClient.InitializedAsync += OnLspClientInitializedAsync;
            _codeWhispererLspClient.RequestConnectionMetadataAsync += OnLspClientRequestConnectionMetadataAsync;
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
                    var connectionProperties = CreateConnectionProperties(credentialIdentifier);

                    if (!_tokenProvider.TryGetSsoToken(credentialIdentifier, connectionProperties.Region, out var awsToken))
                    {
                        var msg = $"Cannot sign in to CodeWhisperer.  Unable to resolve bearer token {displayName}.";
                        NotifyErrorAndDisconnect(msg);
                        throw new InvalidOperationException(msg);
                    }

                    if (!await UseAwsTokenAsync(awsToken, connectionProperties))
                    {
                        var msg = "Credentials are valid, but the bearer token could not be sent to the language server. You will be signed out, try again.";
                        NotifyErrorAndDisconnect(msg);
                        throw new InvalidOperationException(msg);
                    }

                    toolkitContext.ToolkitHost.OutputToHostConsole($"Signed in to CodeWhisperer with {displayName}.");
                    await SaveCredentialIdAsync(credentialIdentifier.Id);
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
        /// Creates an object representing properties associated with the connection(credentialId)
        /// </summary>
        private ConnectionProperties CreateConnectionProperties(ICredentialIdentifier credentialIdentifier)
        {
            var toolkitContext = _toolkitContextProvider.GetToolkitContext();
            var profile = toolkitContext.CredentialSettingsManager.GetProfileProperties(credentialIdentifier);
            var region = toolkitContext.RegionProvider.GetRegion(profile.SsoRegion);
            return new ConnectionProperties()
            {
                CredentialIdentifier = credentialIdentifier,
                Region = region,
                SsoStartUrl = profile.SsoStartUrl
            };
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
            var credentialId = _signedInConnectionProperties.CredentialIdentifier;
            var displayName = credentialId?.DisplayName ?? "<unknown>";

            try
            {
                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                if (await ClearAwsTokenAsync())
                {
                    _toolkitContextProvider.GetToolkitContext().CredentialManager.Invalidate(credentialId);
                    toolkitContext.ToolkitHost.OutputToHostConsole($"Signed out of CodeWhisperer with {displayName}.");
                }

                await SaveCredentialIdAsync(null);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to sign out of CodeWhisperer with {displayName}.";
                NotifyError(msg, ex);
                throw new InvalidOperationException(msg, ex);
            }
        }

        /// <summary>
        /// Store the given credentialsId as part of the CodeWhisperer settings.
        /// </summary>
        private async Task SaveCredentialIdAsync(string credentialIdentifierId)
        {
            var settings = await _settingsRepository.GetAsync();
            settings.CredentialIdentifier = credentialIdentifierId;
            _settingsRepository.Save(settings);
        }

        /// <summary>
        /// Raised when the Language Client has completed its initialization handshake with the
        /// language server.
        /// </summary>
        private async Task OnLspClientInitializedAsync(object sender, EventArgs args)
        {
            try
            {
                // If user was signed in to CodeWhisperer in their previous IDE session,
                // attempt to silently re-connect using the same credentials.
                // If user interaction would be required (eg: sso login), remain signed out.
                var settings = await _settingsRepository.GetAsync();

                if (string.IsNullOrWhiteSpace(settings.CredentialIdentifier))
                {
                    // User was previously logged out
                    return;
                }

                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                var credentialId = toolkitContext.CredentialManager.GetCredentialIdentifierById(settings.CredentialIdentifier);

                if (credentialId == null)
                {
                    // Credentials entry no longer exists. Remain in signed-out state.
                    return;
                }

                var connectionProperties = CreateConnectionProperties(credentialId);
                if (!_tokenProvider.TrySilentGetSsoToken(credentialId, connectionProperties.Region, out var token))
                {
                    // Toolkit cannot automatically log in. Remain in signed-out state.
                    return;
                }

                if (!await UseAwsTokenAsync(token, connectionProperties))
                {
                    // Toolkit could not pass token over to language server. Remain in signed-out state.
                    _logger.Error("Failed to update bearer token after language server initialized. Remaining signed out.");
                    return;
                }

                toolkitContext.ToolkitHost.OutputToHostConsole($"Reconnected to CodeWhisperer with {credentialId.DisplayName}");
            }
            catch (Exception e)
            {
                _logger.Error("Unable to auto-login to CodeWhisperer", e);
            }
        }

        /// <summary>
        /// Raised when the language server requests client for information about the auth connection
        /// </summary>
        private Task OnLspClientRequestConnectionMetadataAsync(object sender, ConnectionMetadataEventArgs args)
        {
            var metadata = new ConnectionMetadata()
            {
                SsoProfileData = new SsoProfileData() { StartUrl = _signedInConnectionProperties?.SsoStartUrl }
            };
            args.Response = metadata;
            return Task.CompletedTask;
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
            return UseAwsTokenAsync(null, null);
        }

        /// <summary>
        /// Updates the connection state (and adjusts the refresh timer) based on the given token.
        /// Signed in and refreshed tokens should be passed to this method.
        /// To "clear" a token (sign out), pass null for all parameters, or use <see cref="ClearAwsTokenAsync"/>.
        /// </summary>
        private async Task<bool> UseAwsTokenAsync(
            AWSToken token, ConnectionProperties connectionProperties)
        {
            var credentialsProtocol = _codeWhispererLspClient.CreateToolkitLspCredentials();

            _tokenRefreshTimer.Stop();

            if (token == null)
            {
                // Indicates "sign out"
                credentialsProtocol.DeleteToken();

                _signedInConnectionProperties = null;
                _tokenExpiresAt = null;
                Status = ConnectionStatus.Disconnected;
            }
            else
            {
                // Indicates "sign in" or "token refresh"

                // TODO : Soon UpdateTokenAsync will return a response. Return true or false based on the response.
                await credentialsProtocol.UpdateTokenAsync(new BearerToken() { Token = token.Token });
                _signedInConnectionProperties = connectionProperties;

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
            if (!_tokenProvider.TrySilentGetSsoToken(
                    _signedInConnectionProperties.CredentialIdentifier, _signedInConnectionProperties.Region, out var refreshToken))
            {
                // Unable to refresh the token (or retrieve the current one)
                // Consider the connection expired now.
                await ExpireConnectionAsync();

                return;
            }

            // We either successfully refreshed the token, or AWS SDK gave us the same token back.
            if (refreshToken.ExpiresAt > _tokenExpiresAt)
            {
                // Token Refresh was successful
                if (!await UseAwsTokenAsync(refreshToken, _signedInConnectionProperties))
                {
                    // Unable to update the token on the language server.
                    // If we do nothing, the token will soon expire, and the user would be unable
                    // to use CodeWhisperer. Consider the connection expired now.
                    await ExpireConnectionAsync();
                }
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

        private async Task ExpireConnectionAsync()
        {
            var toolkitContext = _toolkitContextProvider.GetToolkitContext();

            toolkitContext.ToolkitHost.OutputToHostConsole(
                "Connection to CodeWhisperer has expired. Sign in to continue using CodeWhisperer features.");

            // Move the connection state to Expired, then disconnect.
            Status = ConnectionStatus.Expired; // event handlers react to this
            if (!await ClearAwsTokenAsync())
            {
                // Not much else we can do. Things are in a bad state here. We tried to log out.
                _logger.Error("Unexpected failure when clearing the bearer token");
            }
        }

        public void Dispose()
        {
            _codeWhispererLspClient.InitializedAsync -= OnLspClientInitializedAsync;
            _codeWhispererLspClient.RequestConnectionMetadataAsync -= OnLspClientRequestConnectionMetadataAsync;

            _tokenRefreshTimer.Elapsed -= OnTokenRefreshTimerElapsed;
            _tokenRefreshTimer?.Dispose();
        }
    }
}
