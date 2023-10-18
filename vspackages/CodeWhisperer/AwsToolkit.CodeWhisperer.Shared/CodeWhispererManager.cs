using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
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
        private readonly IConnection _connection;
        private readonly ISuggestionProvider _suggestionProvider;
        private readonly IReferenceLogger _referenceLogger;
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;

        [ImportingConstructor]
        public CodeWhispererManager(
            IConnection connection,
            ISuggestionProvider suggestionProvider,
            IReferenceLogger referenceLogger,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _connection = connection;
            _suggestionProvider = suggestionProvider;
            _referenceLogger = referenceLogger;
            _taskFactoryProvider = taskFactoryProvider;
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
        public async Task<IEnumerable<Suggestion>> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            return request.IsAutoSuggestion && await IsAutoSuggestPausedAsync()
                ? Enumerable.Empty<Suggestion>()
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
    }
}
