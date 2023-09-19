using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

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

        [ImportingConstructor]
        public CodeWhispererManager(
            IConnection connection,
            ISuggestionProvider suggestionProvider)
        {
            _connection = connection;
            _suggestionProvider = suggestionProvider;
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
        /// Gets the current status
        /// </summary>
        public ConnectionStatus GetStatus()
        {
            return _connection.GetStatus();
        }

        /// <summary>
        /// Event signaling that the status has changed
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged
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
        /// <param name="isAutoSuggestion">true: the request was auto-triggered, false: the request was user triggered</param>
        // TODO : define the request model
        public async Task<IEnumerable<Suggestion>> GetSuggestionsAsync(bool isAutoSuggestion = true)
        {
            return isAutoSuggestion && await IsAutoSuggestPausedAsync()
                ? Enumerable.Empty<Suggestion>()
                : await _suggestionProvider.GetSuggestionsAsync();
        }
    }
}
