using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Sono;

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

        private readonly IToolkitLspClient _toolkitLspClient;

        [ImportingConstructor]
        public Connection(IToolkitContextProvider toolkitContextProvider, IToolkitLspClient toolkitLspClient)
        {
            _toolkitContextProvider = toolkitContextProvider;
            _toolkitLspClient = toolkitLspClient;
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
        public Task SignInAsync()
        {
            SignInToAwsBuilderId();
            return Task.CompletedTask;
        }

        // TODO IDE-11603 When adding IdC consider if it is another method or subtypes are more appropriate.
        private void SignInToAwsBuilderId()
        {
            try
            {
                var toolkitContext = _toolkitContextProvider.GetToolkitContext();

                toolkitContext.ToolkitHost.OutputToHostConsole("Attempting to update AWS Builder ID bearer token on LSP.");

                // See SonoCredentialProviderFactory.Initialize and SonoCredentialIdentifier ctor for credId name details
                var credId = toolkitContext.CredentialManager.GetCredentialIdentifierById($"{SonoCredentialProviderFactory.FactoryId}:default");
                var region = toolkitContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName);
                var toolkitCreds = toolkitContext.CredentialManager.GetToolkitCredentials(credId, region);

                if (!toolkitCreds.GetTokenProvider().TryResolveToken(out var awsToken))
                {
                    NotifyErrorAndDisconnect("Cannot resolve AWS Builder ID bearer token.");
                    return;
                }

                var lspCreds = _toolkitLspClient.CreateToolkitLspCredentials();
                lspCreds.UpdateToken(new BearerToken() { Token = awsToken.Token });

                Status = ConnectionStatus.Connected;
                toolkitContext.ToolkitHost.OutputToHostConsole("Updated AWS Builder ID bearer token on LSP.");
            }
            catch (Exception ex)
            {
                NotifyErrorAndDisconnect("Failed to update AWS Builder ID bearer token on LSP.", ex);
            }
        }

        /// <summary>
        /// Signs the user out of CodeWhisperer
        /// </summary>
        public Task SignOutAsync()
        {
            // TODO : Implement
            Status = ConnectionStatus.Disconnected;
            return Task.CompletedTask;
        }

        private Task SignOutFromAwsBuilderIdAsync()
        {
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
    }
}
