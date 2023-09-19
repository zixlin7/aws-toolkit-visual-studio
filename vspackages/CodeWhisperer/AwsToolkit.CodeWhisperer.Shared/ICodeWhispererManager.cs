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
        /// Gets the current status
        /// </summary>
        ConnectionStatus GetStatus();

        /// <summary>
        /// Event signaling that the status has changed
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;

        /// <summary>
        /// Pauses automatic code suggestions
        /// </summary>
        Task PauseAutoSuggestAsync();

        /// <summary>
        /// Resumes automatic code suggestions
        /// </summary>
        Task ResumeAutoSuggestAsync();

        Task<bool> IsAutoSuggestPausedAsync();

        /// <summary>
        /// Event signalling a state change for whether or not automatic code suggestions are paused
        /// </summary>
        event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;

        /// <summary>
        /// Queries code suggestions from CodeWhisperer
        /// </summary>
        /// <param name="isAutoSuggestion">true: the request was auto-triggered, false: the request was user triggered</param>
        Task<IEnumerable<Suggestion>> GetSuggestionsAsync(bool isAutoSuggestion = true); // TODO : define the request model
    }
}
