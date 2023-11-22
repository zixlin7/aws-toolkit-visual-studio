using System;
using System.Threading.Tasks;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class FakeSuggestionProvider : ISuggestionProvider
    {
        public bool PauseAutomaticSuggestions = false;
        public SuggestionSession SuggestionSession = new SuggestionSession();

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

        public virtual Task<SuggestionSession> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            return Task.FromResult(SuggestionSession);
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
