using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScan;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

namespace Amazon.AwsToolkit.CodeWhisperer
{
    /// <summary>
    /// The main CodeWhisperer controller. Other components should orchestrate features through this one.
    /// This class is intended to abstract and reduce coupling between other components.
    /// Calling code does not need to have knowledge of state, for example, if code recommendations are
    /// requested, but the state is paused, the system will behave as if there are no recommendations
    /// available.
    /// </summary>
    [Export(typeof(ICodeWhispererManager))]
    internal class CodeWhispererManager : ICodeWhispererManager
    {
        private readonly ICodeWhispererLspClient _lspClient;
        private readonly IConnection _connection;
        private readonly ISuggestionProvider _suggestionProvider;
        private readonly IReferenceLogger _referenceLogger;
        private readonly ISecurityScanProvider _securityScanProvider;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;

        [ImportingConstructor]
        public CodeWhispererManager(
            ICodeWhispererLspClient lspClient,
            IConnection connection,
            ISuggestionProvider suggestionProvider,
            IReferenceLogger referenceLogger,
            ISecurityScanProvider securityScanProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _lspClient = lspClient;
            _connection = connection;
            _suggestionProvider = suggestionProvider;
            _referenceLogger = referenceLogger;
            _securityScanProvider = securityScanProvider;
            _taskFactoryProvider = taskFactoryProvider;
        }

        /// <summary>
        /// Gets the current status of the LSP Client
        /// </summary>
        public LspClientStatus ClientStatus => _lspClient.Status;

        /// <summary>
        /// Event signaling that the status of the LSP Client has changed
        /// </summary>
        public event EventHandler<LspClientStatusChangedEventArgs> ClientStatusChanged
        {
            add => _lspClient.StatusChanged += value;
            remove => _lspClient.StatusChanged -= value;
        }

        /// <summary>
        /// Attempts to connect with the user's credentials
        /// </summary>
        public async Task SignInAsync()
        {
            await _connection.SignInAsync();
        }

        /// <summary>
        /// Disconnects from the user's credentials
        /// </summary>
        public async Task SignOutAsync()
        {
            await _connection.SignOutAsync();
        }

        /// <summary>
        /// Gets the current connection status
        /// </summary>
        public ConnectionStatus ConnectionStatus => _connection.Status;

        /// <summary>
        /// Event signaling that the connection status has changed
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged
        {
            add => _connection.StatusChanged += value;
            remove => _connection.StatusChanged -= value;
        }

        /// <summary>
        /// Pauses automatic code suggestions
        /// </summary>
        public async Task PauseAutoSuggestAsync()
        {
            await _suggestionProvider.PauseAutoSuggestAsync();
        }

        /// <summary>
        /// Resumes automatic code suggestions
        /// </summary>
        public async Task ResumeAutoSuggestAsync()
        {
            await _suggestionProvider.ResumeAutoSuggestAsync();
        }

        /// <summary>
        /// Use <see cref="IsAutoSuggestPausedAsync"/> when possible.
        /// This is provided for places where async is not an option, like in ctor and "can execute" calls,
        /// and encapsulates blocking the current thread to run the async version.
        /// </summary>
        public bool IsAutoSuggestPaused()
        {
            return _taskFactoryProvider.JoinableTaskFactory.Run(async () => await IsAutoSuggestPausedAsync());
        }

        public async Task<bool> IsAutoSuggestPausedAsync()
        {
            return await _suggestionProvider.IsAutoSuggestPausedAsync();
        }

        /// <summary>
        /// Event signalling a state change for whether or not automatic code suggestions are paused
        /// </summary>
        public event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged
        {
            add => _suggestionProvider.PauseAutoSuggestChanged += value;
            remove => _suggestionProvider.PauseAutoSuggestChanged -= value;
        }

        /// <summary>
        /// Queries code suggestions from CodeWhisperer
        /// </summary>
        public async Task<SuggestionSession> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            return request.IsAutoSuggestion && await IsAutoSuggestPausedAsync()
                ? new SuggestionSession()
                : await _suggestionProvider.GetSuggestionsAsync(request);
        }

        public async Task ShowReferenceLoggerAsync()
        {
            await _referenceLogger.ShowAsync();
        }

        public async Task LogReferenceAsync(LogReferenceRequest request)
        {
            await _referenceLogger.LogReferenceAsync(request);
        }

        public SecurityScanState SecurityScanState => _securityScanProvider.ScanState;

        public event EventHandler<SecurityScanStateChangedEventArgs> SecurityScanStateChanged
        {
            add => _securityScanProvider.SecurityScanStateChanged += value;
            remove => _securityScanProvider.SecurityScanStateChanged -= value;
        }

        public async Task ScanAsync()
        {
            await _securityScanProvider.ScanAsync();
        }

        public async Task CancelScanAsync()
        {
            await _securityScanProvider.CancelScanAsync();
        }

        public async Task SendSessionCompletionResultAsync(LogInlineCompletionSessionResultsParams resultParams)
        {
            var sessionResultsPublisher = _lspClient.CreateSessionResultsPublisher();
            await sessionResultsPublisher.SendInlineCompletionSessionResultAsync(resultParams);
        }
    }
}
