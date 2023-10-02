using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    /// <summary>
    /// Maintains the list of suggestions, index of the current suggestion, and creates Proposals using them.
    /// Defines the description text based on the contents of the suggestion (Suggestion 1/5, etc...)
    /// </summary>
    public class SuggestionContainer : SuggestionBase
    {
        private int _currentSuggestionIndex = 0;
        private readonly Suggestion[] _suggestions;
        private readonly CancellationToken _disposalToken;
        private readonly IWpfTextView _view;

        public SuggestionContainer(IEnumerable<Suggestion> suggestions,
            IWpfTextView view,
            CancellationToken disposalToken)
        {
            _suggestions = suggestions.ToArray();
            _view = view;
            _disposalToken = disposalToken;
            CurrentProposal = CreateProposal(_currentSuggestionIndex);
        }

        public Proposal CurrentProposal { get; private set; }

        public override EditDisplayStyle EditStyle => EditDisplayStyle.GrayText;
        public override TipStyle TipStyle => TipStyle.AlwaysShowTip | TipStyle.TipTopRightJustifiedPlacement;

        // TODO: add metrics for these user actions
        public override Task OnAcceptedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForAccept reason, CancellationToken cancel)
        {
            // TODO (IDE-11380): reconcile any reference tracking
            return Task.CompletedTask;
        }

        public override Task OnDismissedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForDismiss reason, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public override Task OnProposalUpdatedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForUpdate reason, VirtualSnapshotPoint caret, CompletionState completionState, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        public override async Task OnChangeProposalAsync(
            SuggestionSessionBase session,
            ProposalBase originalProposal, ProposalBase currentProposal,
            bool forward, CancellationToken cancel)
        {
            if (forward)
            {
                NextSuggestion();
            }
            else
            {
                PreviousSuggestion();
            }

            await session.DisplayProposalAsync(CurrentProposal, _disposalToken);
        }

        public override bool HasMultipleSuggestions => _suggestions.Length > 1;

#pragma warning disable CS0067 // The event 'SuggestionContainer.PropertyChanged' is never used
        public override event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067 // The event 'SuggestionContainer.PropertyChanged' is never used

        private void NextSuggestion()
        {
            _currentSuggestionIndex = (_currentSuggestionIndex + 1) % _suggestions.Length;
            CurrentProposal = CreateProposal(_currentSuggestionIndex);
        }

        private void PreviousSuggestion()
        {
            _currentSuggestionIndex = (_currentSuggestionIndex - 1 + _suggestions.Length) % _suggestions.Length;
            CurrentProposal = CreateProposal(_currentSuggestionIndex);
        }

        private Proposal CreateProposal(int suggestionIndex)
        {
            if (suggestionIndex >= _suggestions.Length)
            {
                return null;
            }

            var suggestion = _suggestions[suggestionIndex];

            return new Proposal($"Suggestion {suggestionIndex + 1} / {_suggestions.Length}",
                GetProposedEdits(suggestion.Text), _view.Caret.Position.VirtualBufferPosition, null,ProposalFlags.ShowCommitHighlight);
        }

        private List<ProposedEdit> GetProposedEdits(string text)
        {
            return _view.GetMultiSelectionBroker().AllSelections.Select(selection => new ProposedEdit(selection.Extent.SnapshotSpan, text, null)).ToList();
        }
    }
}
