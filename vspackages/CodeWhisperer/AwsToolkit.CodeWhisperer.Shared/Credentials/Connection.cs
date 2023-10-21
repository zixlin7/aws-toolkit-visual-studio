using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    /// <summary>
    /// Handles the Connection state and operations for CodeWhisperer features.
    /// </summary>
    [Export(typeof(IConnection))]
    internal class Connection : IConnection
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Connection));

        private readonly IToolkitContextProvider _toolkitContextProvider;

        private readonly IToolkitLspClient _codeWhispererLspClient;

        private ICredentialIdentifier _signedInCredentialIdentifier;

        [ImportingConstructor]
        public Connection(IToolkitContextProvider toolkitContextProvider, ICodeWhispererLspClient codeWhispererLspClient)
        {
            _toolkitContextProvider = toolkitContextProvider;
            _codeWhispererLspClient = codeWhispererLspClient;
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

                    var credentialsProtocol = _codeWhispererLspClient.CreateToolkitLspCredentials();
                    await credentialsProtocol.UpdateTokenAsync(new BearerToken() { Token = awsToken.Token });

                    _signedInCredentialIdentifier = credentialIdentifier;
                    Status = ConnectionStatus.Connected;
                    toolkitContext.ToolkitHost.OutputToHostConsole($"Signed in to CodeWhisperer with {displayName}.");
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
        public Task SignOutAsync()
        {
            var displayName = _signedInCredentialIdentifier?.DisplayName ?? "<unknown>";

            try
            {
                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                var lspCreds = _codeWhispererLspClient.CreateToolkitLspCredentials();
                lspCreds.DeleteToken();

                _toolkitContextProvider.GetToolkitContext().CredentialManager.Invalidate(_signedInCredentialIdentifier);

                _signedInCredentialIdentifier = null;
                Status = ConnectionStatus.Disconnected;
                toolkitContext.ToolkitHost.OutputToHostConsole($"Sign out of CodeWhisperer with {displayName}.");

                return Task.CompletedTask;
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
    }
}
