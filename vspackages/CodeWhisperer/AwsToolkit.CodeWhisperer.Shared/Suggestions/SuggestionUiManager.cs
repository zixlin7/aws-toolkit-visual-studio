using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

#pragma warning disable CS0618 // Type or member is obsolete
namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    [Export(typeof(ISuggestionUiManager))]
    internal class SuggestionUiManager : ISuggestionUiManager, IDisposable
    {
        private readonly SuggestionServiceBase _serviceBase;
        private readonly SuggestionProviderBase _suggestionProviderBase = new SuggestionProviderBase();
        private SuggestionSessionBase _session;
        private readonly Dictionary<IWpfTextView, SuggestionManagerBase> _textViewManagers =
            new Dictionary<IWpfTextView, SuggestionManagerBase>();
        private readonly ToolkitJoinableTaskFactoryProvider _taskFactoryProvider;
        private readonly ICodeWhispererManager _manager;
        private SuggestionContainer _suggestionContainer;
     
        [ImportingConstructor]
        public SuggestionUiManager(ICodeWhispererManager manager, SVsServiceProvider serviceProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider, SuggestionServiceBase serviceBase)
        {
            _manager = manager;

            _serviceBase = serviceBase;
            
            _taskFactoryProvider = taskFactoryProvider;

            _serviceBase.ProposalDisplayed += OnProposalDisplayed;
        }

        public bool IsSuggestionDisplayed(ICodeWhispererTextView textView)
        {
            return _textViewManagers.ContainsKey(textView.GetWpfTextView())
                   && _textViewManagers[textView.GetWpfTextView()].IsSuggestionDisplayed;
        }

        public async Task ShowAsync(IEnumerable<Suggestion> suggestions,
            SuggestionInvocationProperties invocationProperties, ICodeWhispererTextView view)
        {
            var suggestionManager = await CreateSuggestionManagerAsync(view.GetWpfTextView());

            if (_session != null)
            {
                await _session.DismissAsync(ReasonForDismiss.DismissedBySession, _taskFactoryProvider.DisposalToken);
                _suggestionContainer = null;
                _session = null;
            }

            if (suggestionManager != null)
            {
                _suggestionContainer =
                    new SuggestionContainer(suggestions, invocationProperties, view, _manager, _taskFactoryProvider.DisposalToken);
                await _suggestionContainer.SetInitialTypedPrefixAsync();

                _session = await suggestionManager.TryDisplaySuggestionAsync(_suggestionContainer,
                    _taskFactoryProvider.DisposalToken);

                if (_session != null)
                {
                    // Filters the proposal using the most recent caret position
                    if (await _suggestionContainer.FilterSuggestionsAsync(_session))
                    {
                        await _session.DisplayProposalAsync(_suggestionContainer.CurrentProposal, _taskFactoryProvider.DisposalToken);
                    }
                    else
                    {
                        await _session.DismissAsync(ReasonForDismiss.DismissedBySession, _taskFactoryProvider.DisposalToken);
                    }
                }
            }
        }

        private void OnProposalDisplayed(object sender, ProposalDisplayedEventArgs e)
        {
            if (_suggestionContainer != null)
            {
                _suggestionContainer.OnProposalDisplayed(e.OriginalProposal.ProposalId);
            }
        }

        private async Task<SuggestionManagerBase> CreateSuggestionManagerAsync(IWpfTextView textView)
        {
            if (_textViewManagers.Count != 0 && _textViewManagers.TryGetValue(textView, out var suggestionManager))
            {
                return suggestionManager;
            }

            var manager = await _serviceBase.TryRegisterProviderAsync(_suggestionProviderBase, textView,
                nameof(SuggestionUiManager), _taskFactoryProvider.DisposalToken);

            _textViewManagers.Add(textView, manager);

            return _textViewManagers[textView];
        }

        public void Dispose()
        {
            _taskFactoryProvider.JoinableTaskFactory.Run(async () =>
            {
                foreach (var item in _textViewManagers)
                {
                    await item.Value.DisposeAsync();
                }
                _textViewManagers.Clear();
            });
            if (_serviceBase != null)
            {
                _serviceBase.ProposalDisplayed -= OnProposalDisplayed;
            }
        }
    }
}
