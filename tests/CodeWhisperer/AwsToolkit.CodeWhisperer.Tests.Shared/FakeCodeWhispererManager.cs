using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions;
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
        public bool DidShowReferenceLogger = false;
        public readonly List<LogReferenceRequest> LoggedReferences = new List<LogReferenceRequest>();
        public readonly SuggestionSession SuggestionSession = new SuggestionSession();
         public LogInlineCompletionSessionResultsParams SessionResultsParam =
            new LogInlineCompletionSessionResultsParams();

        public LspClientStatus ClientStatus { get; set; } = LspClientStatus.SettingUp;

        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        public event EventHandler<PauseStateChangedEventArgs> PauseAutoSuggestChanged;
        public event EventHandler<LspClientStatusChangedEventArgs> ClientStatusChanged;

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

        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;

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

        public virtual bool IsAutoSuggestPaused()
        {
            return PauseAutomaticSuggestions;
        }

        public virtual Task<bool> IsAutoSuggestPausedAsync()
        {
            return Task.FromResult(PauseAutomaticSuggestions);
        }

        public virtual Task<SuggestionSession> GetSuggestionsAsync(GetSuggestionsRequest request)
        {
            return Task.FromResult(SuggestionSession);
        }

        public void RaiseStatusChanged()
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(ConnectionStatus));
        }

        public void RaisePauseAutoSuggestChanged()
        {
            PauseAutoSuggestChanged?.Invoke(this, new PauseStateChangedEventArgs()
            {
                IsPaused = PauseAutomaticSuggestions,
            });
        }

        public void RaiseClientStatusChanged()
        {
            ClientStatusChanged?.Invoke(this, new LspClientStatusChangedEventArgs(ClientStatus));
        }

        public Task ShowReferenceLoggerAsync()
        {
            DidShowReferenceLogger = true;
            return Task.CompletedTask;
        }

        public Task LogReferenceAsync(LogReferenceRequest request)
        {
            LoggedReferences.Add(request);
            return Task.CompletedTask;
        }

        public Task SendSessionCompletionResultAsync(LogInlineCompletionSessionResultsParams resultParams)
        {
            SessionResultsParam = resultParams;
            return Task.CompletedTask;
        }
    }
}
