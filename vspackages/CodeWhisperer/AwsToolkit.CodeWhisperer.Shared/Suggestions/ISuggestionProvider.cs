using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    /// <summary>
    /// Handles querying for code suggestions
    /// </summary>
    public interface ISuggestionProvider : IDisposable
    {
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
        Task<IEnumerable<Suggestion>> GetSuggestionsAsync(); // TODO : IDE-11522 : define the request model
    }
}
