using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class FakeSuggestionProvider : ISuggestionProvider
    {
        public readonly List<Suggestion> Suggestions = new List<Suggestion>();
        public bool PauseAutomaticSuggestions = false;

        public virtual Task PauseAutoSuggestAsync()
        {
            PauseAutomaticSuggestions = true;
            return Task.CompletedTask;
        }

        public virtual Task ResumeAutoSuggestAsync()
        {
            PauseAutomaticSuggestions = false;
            return Task.CompletedTask;
        }

        public virtual Task<bool> IsAutoSuggestPausedAsync()
        {
            return Task.FromResult(PauseAutomaticSuggestions);
        }

        public event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;

        public virtual Task<IEnumerable<Suggestion>> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            return Task.FromResult<IEnumerable<Suggestion>>(Suggestions);
        }

        public void RaisePauseAutoSuggestChanged()
        {
            PauseAutoSuggestChanged?.Invoke(this, new PauseStateChangedEventArgs()
            {
                IsPaused = PauseAutomaticSuggestions,
            });
        }

        public void Dispose()
        {
        }
    }
}
