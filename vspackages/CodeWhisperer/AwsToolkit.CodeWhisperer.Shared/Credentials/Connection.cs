using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;

using log4net;

using Microsoft.VisualStudio.Threading;

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

        internal ConnectionProperties _signedInConnectionProperties;
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

            _settingsRepository.SettingsSaved += OnSettingsRepositorySettingsSaved;
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

        private void OnSettingsRepositorySettingsSaved(object sender, CodeWhispererSettingsSavedEventArgs e)
        {
            if (_codeWhispererLspClient.Status != LspClientStatus.Running)
            {
                // The language server doesn't have a protocol channel for us to work with
                // Sign-in will automatically kick in the next time the language server starts up.
                return;
            }

            var credentialIdentifierSetting = e.Settings.CredentialIdentifier;

            if (credentialIdentifierSetting != _signedInConnectionProperties?.CredentialIdentifier?.Id)
            {
                _taskFactoryProvider.JoinableTaskFactory.Run(async () =>
                {
                    try
                    {
                        if (_signedInConnectionProperties != null)
                        {
                            await SignOutAsync(false, false);
                        }

                        var credentialIdentifier = _toolkitContextProvider.GetToolkitContext().CredentialManager
                            .GetCredentialIdentifierById(credentialIdentifierSetting);

                        if (credentialIdentifier != null)
                        {
                            await SignInAsync(credentialIdentifier, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Unable to update signed-in credentials.", ex);
                    }
                });
            }
        }

        /// <summary>
        /// Connects the user to CodeWhisperer.
        /// User may go through modal dialogs and login flows as a result.
        /// </summary>
        public async Task SignInAsync()
        {
            var credentialIdentifier = await PromptUserForCredentialIdAsync();

            if (credentialIdentifier != null)
            {
                await SignInAsync(credentialIdentifier, true);
                RecordAuthAddConnectionMetric(new ActionResults().WithSuccess(true), credentialIdentifier);
            }
            else
            {
                RecordAuthAddConnectionMetric(new ActionResults().WithCancelled(true), credentialIdentifier);
            }
        }

        internal async Task SignInAsync(ICredentialIdentifier credentialIdentifier, bool saveSettings)
        { 
            var displayName = credentialIdentifier?.DisplayName ?? "unknown";

            try
            {
                // Ensure we aren't on the UI thread before getting an SSO Token.
                // Otherwise, the IDE will freeze while user is in the browser login flow.
                await TaskScheduler.Default;

                var toolkitContext = _toolkitContextProvider.GetToolkitContext();
                var connectionProperties = CreateConnectionProperties(credentialIdentifier);

                if (!_tokenProvider.TryGetSsoToken(credentialIdentifier, connectionProperties.Region, out var awsToken))
                {
                    var msg = $"Cannot sign in to CodeWhisperer, try again.{Environment.NewLine}Unable to resolve bearer token {displayName}.";

                    // Invalidate the token to avoid an infinite loop of sign-in failures
                    // that could otherwise require user to delete their token cache in
                    // order to break the loop.
                    //
                    // There are normal circumstances where we get here, like when
                    // the user presses Cancel on the SSO Token confirmation dialog.
                    // There are unexpected circumstances, like if the AWS SDK raises
                    // an exception while handling the token cache:
                    // https://github.com/aws/aws-sdk-net/pull/3083/files#r1389714680
                    InvalidateSsoToken(credentialIdentifier);
                    throw new InvalidOperationException(msg);
                }

                if (!await UseAwsTokenAsync(awsToken, connectionProperties))
                {
                    var msg = "Credentials are valid, but the bearer token could not be sent to the language server. You will be signed out, try again.";
                    throw new InvalidOperationException(msg);
                }

                toolkitContext.ToolkitHost.OutputToHostConsole($"Signed in to CodeWhisperer with {displayName}.");

                if (saveSettings)
                {
                    await SaveCredentialIdAsync(credentialIdentifier.Id);
                }
            }
            catch (Exception ex)
            {
                RecordAuthAddConnectionMetric(ActionResults.CreateFailed(ex), credentialIdentifier);

                var title = $"Failed to sign in to CodeWhisperer with {displayName}.";
                NotifyErrorAndDisconnect(title, ex);
                throw new InvalidOperationException(title, ex);
            }
        }

        public void RecordAuthAddConnectionMetric(ActionResults actionResults, ICredentialIdentifier credentialIdentifier)
        {
            var toolkitContext = _toolkitContextProvider.GetToolkitContext();

            var data = actionResults.CreateMetricData<AuthAddConnection>(MetadataValue.NotApplicable, MetadataValue.NotApplicable);
            data.Attempts = 1;
            data.FeatureId = FeatureId.Codewhisperer;
            data.IsAggregated = false;
            data.Result = actionResults.AsTelemetryResult();
            data.Source = "statusBarBrain";

            if (credentialIdentifier != null)
            {
                data.CredentialSourceId = credentialIdentifier.ProfileName == SonoCredentialProviderFactory.CodeWhispererProfileName ?
                    CredentialSourceId.AwsId :
                    CredentialSourceId.IamIdentityCenter;
            }

            toolkitContext.TelemetryLogger.RecordAuthAddConnection(data);
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
        protected virtual async Task<ICredentialIdentifier> PromptUserForCredentialIdAsync()
        {
            var viewModel = new CredentialSelectionDialogViewModel(_toolkitContextProvider, _settingsRepository);
            viewModel.CredentialIdentifiers.AddAll(GetCodeWhispererCredentialIdentifiers());
            await viewModel.InitializeAsync();

            var dlg = new CredentialSelectionDialog
            {
                DataContext = viewModel
            };

            return dlg.ShowModal() == true ? viewModel.SelectedCredentialIdentifier : null;
        }

        internal IEnumerable<ICredentialIdentifier> GetCodeWhispererCredentialIdentifiers()
        {
            var tkc = _toolkitContextProvider.GetToolkitContext();
            var csm = tkc.CredentialSettingsManager;

            return tkc.CredentialManager.GetCredentialIdentifiers()
                .Where(id =>
                {
                    var scopes = csm.GetProfileProperties(id).SsoRegistrationScopes;
                    return
                        csm.GetCredentialType(id) == AWSToolkit.Credentials.Utils.CredentialType.BearerToken
                        && scopes?.Length >= 2
                        && scopes.Contains(SonoProperties.CodeWhispererAnalysisScope)
                        && scopes.Contains(SonoProperties.CodeWhispererCompletionsScope);
                })
                .OrderBy(id => id.ProfileName);
        }

        /// <summary>
        /// Signs the user out of CodeWhisperer
        /// </summary>
        public async Task SignOutAsync()
        {
            await SignOutAsync(true, true);
        }

        internal async Task SignOutAsync(bool saveSettings, bool invalidateSsoToken)
        {
            var credentialId = _signedInConnectionProperties.CredentialIdentifier;
            var displayName = credentialId?.DisplayName ?? "<unknown>";

            try
            {
                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                if (await ClearAwsTokenAsync())
                {
                    if (invalidateSsoToken)
                    {
                        InvalidateSsoToken(credentialId);
                    }
                    toolkitContext.ToolkitHost.OutputToHostConsole($"Signed out of CodeWhisperer with {displayName}.");
                }

                if (saveSettings)
                {
                    await SaveCredentialIdAsync(null);
                }
            }
            catch (Exception ex)
            {
                var title = $"Failed to sign out of CodeWhisperer with {displayName}.";
                NotifyError(title, ex);
                throw new InvalidOperationException(title, ex);
            }
        }

        /// <summary>
        /// Makes an attempt to remove the credential's SSO Token from cache, but does not
        /// raise an error in the call stack.
        /// </summary>
        private void InvalidateSsoToken(ICredentialIdentifier credentialId)
        {
            try
            {
                _toolkitContextProvider.GetToolkitContext().CredentialManager.Invalidate(credentialId);
            }
            catch (Exception exception)
            {
                // Deliberately swallow the error. Code that is calling this method is already performing
                // error-handling processes, which we do not want to interrupt.
                _logger.Error($"Failure trying to invalidate cached SSO token for {credentialId?.Id}. SSO Sign-in might fail unless the token is deleted from cache.", exception);
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
                Sso = new SsoProfileData() { StartUrl = _signedInConnectionProperties?.SsoStartUrl }
            };
            args.Response = metadata;
            return Task.CompletedTask;
        }

        private void NotifyErrorAndDisconnect(string title, Exception ex)
        {
            NotifyError(title, ex);
            Status = ConnectionStatus.Disconnected;
        }

        private void NotifyError(string title, Exception ex)
        {
            _logger.Error(title, ex);

            var toolkitContext = _toolkitContextProvider.GetToolkitContext();
            toolkitContext.ToolkitHost.OutputToHostConsole(title);
            toolkitContext.ToolkitHost.OutputToHostConsole(ex.Message);
            toolkitContext.ToolkitHost.ShowError(title, ex.Message ?? title);
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

                // set signed in state before sending the updated token to server because request(and subsequent handling) to get additional metadata is made before control is returned to this class
                _signedInConnectionProperties = connectionProperties;

                // TODO : Soon UpdateTokenAsync will return a response. Return true or false based on the response.
                try
                {
                    await credentialsProtocol.UpdateTokenAsync(new BearerToken() { Token = token.Token });
                }
                catch (Exception)
                {
                    // catch exception when sending token to server only to clear out signed in state set previously
                    _signedInConnectionProperties = null;
                    throw;
                }

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
            if (_codeWhispererLspClient.Status != LspClientStatus.Running)
            {
                // Don't bother trying to refresh the token if the language server is not running
                return;
            }

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
            _settingsRepository.SettingsSaved -= OnSettingsRepositorySettingsSaved;

            _codeWhispererLspClient.InitializedAsync -= OnLspClientInitializedAsync;
            _codeWhispererLspClient.RequestConnectionMetadataAsync -= OnLspClientRequestConnectionMetadataAsync;

            _tokenRefreshTimer.Elapsed -= OnTokenRefreshTimerElapsed;
            _tokenRefreshTimer?.Dispose();
        }
    }
}
