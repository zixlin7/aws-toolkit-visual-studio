using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Models.Text;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;

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
        private readonly ICodeWhispererTextView _view;
        private readonly ICodeWhispererManager _manager;

        public SuggestionContainer(IEnumerable<Suggestion> suggestions,
            ICodeWhispererTextView view,
            ICodeWhispererManager manager,
            CancellationToken disposalToken)
        {
            _suggestions = suggestions.ToArray();
            _view = view;
            _manager = manager;
            _disposalToken = disposalToken;

            Elements = new[]
            {
                new TipElement(
                    KnownMonikers.Settings.ToImageId(),
                    null,
                    typeof(VSConstants.VSStd97CmdID).GUID,
                    (uint) VSConstants.VSStd97CmdID.ToolsOptions,
                    commandArg: typeof(CodeWhispererSettingsProvider).GUID.ToString())
            };

            CurrentProposal = CreateProposal(_currentSuggestionIndex);
        }

        public Proposal CurrentProposal { get; private set; }

        public override EditDisplayStyle EditStyle => EditDisplayStyle.GrayText;
        public override TipStyle TipStyle => TipStyle.AlwaysShowTip | TipStyle.TipTopRightJustifiedPlacement;

        // TODO: add metrics for these user actions
        public override async Task OnAcceptedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForAccept reason, CancellationToken cancel)
        {
            if (_currentSuggestionIndex >= _suggestions.Length)
            {
                return;
            }

            var suggestion = _suggestions[_currentSuggestionIndex];

            var cursorPosition = currentProposal.Caret.Position.Position;

            await LogReferencesAsync(suggestion, cursorPosition);
        }

        private async Task LogReferencesAsync(Suggestion suggestion, int cursorPosition)
        {
            if (suggestion.References?.Count > 0)
            {
                var filePath = _view.GetFilePath();

                foreach (var reference in suggestion.References)
                {
                    var referencePosition = cursorPosition + reference.StartIndex;
                    var position = await _view.GetDocumentPositionAsync(referencePosition);
                    await LogReferenceAsync(suggestion, reference, filePath, position);
                }
            }
        }

        private async Task LogReferenceAsync(Suggestion suggestion, SuggestionReference reference, string filePath,
            Position position)
        {
            var logReferenceRequest = new LogReferenceRequest()
            {
                Suggestion = suggestion,
                SuggestionReference = reference,
                Filename = filePath,
                Position = position,
            };

            await _manager.LogReferenceAsync(logReferenceRequest);
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
        public override IReadOnlyList<TipElement> Elements { get; }
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
            var description = CreateProposalDescription(suggestion, suggestionIndex);
            return _view.CreateProposal(suggestion.Text, description);
        }

        private string CreateProposalDescription(Suggestion suggestion, int suggestionIndex)
        {
            // Produce a description that looks like:
            // When there is no licenses: "Suggestion 3 / 4"
            // When there are licenses: "Suggestion (License: Foo) 3 / 4"

            var chunks = new List<string>() { "Suggestion", };

            var licenses = suggestion.References?
                .Select(r => r.LicenseName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(name => name)
                .ToArray();

            if (licenses?.Length > 0)
            {
                // Produce a string like "(License: Foo)" or "(Licenses: Bar, Baz)"
                var licenseHeading = licenses.Length > 1 ? "Licenses" : "License";
                chunks.Add($"({licenseHeading}: {string.Join(", ", licenses)})");
            }

            // We signal that there is pagination by displaying the current position within all suggestions
            chunks.Add($"{suggestionIndex + 1} / {_suggestions.Length}");

            return string.Join(" ", chunks);
        }
    }
}
