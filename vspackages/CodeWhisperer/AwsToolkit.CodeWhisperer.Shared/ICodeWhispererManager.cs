using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer
{
    public interface ICodeWhispererManager
    {
        /// <summary>
        /// Attempts to connect with the user's credentials
        /// </summary>
        Task SignInAsync();

        /// <summary>
        /// Disconnects from the user's credentials
        /// </summary>
        Task SignOutAsync();

        /// <summary>
        /// Gets the current connection status
        /// </summary>
        ConnectionStatus ConnectionStatus { get; }

        /// <summary>
        /// Event signaling that the connection status has changed
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        /// <summary>
        /// Pauses automatic code suggestions
        /// </summary>
        Task PauseAutoSuggestAsync();

        /// <summary>
        /// Resumes automatic code suggestions
        /// </summary>
        Task ResumeAutoSuggestAsync();

        /// <summary>
        /// Use <see cref="IsAutoSuggestPausedAsync"/> when possible.
        /// This is provided for places where async is not an option, like in ctor and "can execute" calls,
        /// and encapsulates blocking the current thread to run the async version.
        /// </summary>
        bool IsAutoSuggestPaused();

        Task<bool> IsAutoSuggestPausedAsync();

        /// <summary>
        /// Event signalling a state change for whether or not automatic code suggestions are paused
        /// </summary>
        event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;

        /// <summary>
        /// Queries code suggestions from CodeWhisperer
        /// </summary>
        Task<IEnumerable<Suggestion>> GetSuggestionsAsync(GetSuggestionsRequest request);
    }
}
