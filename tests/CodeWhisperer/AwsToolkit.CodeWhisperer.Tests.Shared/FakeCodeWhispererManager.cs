using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests
{
    /// <summary>
    /// A fake implementation for <see cref="ICodeWhispererManager"/> that can be used in tests
    /// exercising other CodeWhisperer components.
    /// </summary>
    public class FakeCodeWhispererManager : ICodeWhispererManager
    {
        public bool IsSignedIn = false;
        public bool PauseAutomaticSuggestions = false;
        public ConnectionStatus ConnectionStatus = ConnectionStatus.Disconnected;
        public readonly List<Suggestion> Suggestions = new List<Suggestion>();

        public event EventHandler<ConnectionStatusChangedEventArgs> StatusChanged;
        public event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;

        public virtual Task SignInAsync()
        {
            IsSignedIn = true;
            return Task.CompletedTask;
        }

        public virtual Task SignOutAsync()
        {
            IsSignedIn = false;
            return Task.CompletedTask;
        }

        public ConnectionStatus GetStatus()
        {
            return ConnectionStatus;
        }

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

        public virtual Task<IEnumerable<Suggestion>> GetSuggestionsAsync(bool isAutoSuggestion = true)
        {
            return Task.FromResult<IEnumerable<Suggestion>>(Suggestions);
        }

        public void RaiseStatusChanged()
        {
            StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs()
            {
                ConnectionStatus = ConnectionStatus,
            });
        }

        public void RaisePauseAutoSuggestChanged()
        {
            PauseAutoSuggestChanged?.Invoke(this, new PauseStateChangedEventArgs()
            {
                IsPaused = PauseAutomaticSuggestions,
            });
        }
    }
}
