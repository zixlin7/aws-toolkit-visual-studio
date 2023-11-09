using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Models.Text;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.VsSdk.Common.Documents;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    /// <summary>
    /// Maintains the list of suggestions, index of the current suggestion, and creates Proposals using them.
    /// Defines the description text based on the contents of the suggestion (Suggestion 1/5, etc...)
    /// </summary>
    public class SuggestionContainer : SuggestionBase
    {
        private readonly SuggestionInvocationProperties _invocationProperties;
        private readonly CancellationToken _disposalToken;
        private readonly ICodeWhispererTextView _view;
        private readonly ICodeWhispererManager _manager;
        private readonly List<Suggestion> _unmodifiedSuggestions;
        private Suggestion[] _filteredSuggestions;
        private int _currentSuggestionIndex; // index of the displayed suggestion from _filteredSuggestions
        private int _filteredSuggestionTextStartIndex; // the current suggestion's filtered text start index

        private static readonly List<ReasonForUpdate> _divergenceReasons = new List<ReasonForUpdate>()
        {
            ReasonForUpdate.Diverged, ReasonForUpdate.DivergedAfterBackspace, ReasonForUpdate.DivergedAfterCompletionChange,
            ReasonForUpdate.DivergedAfterCompletionItemCommitted, ReasonForUpdate.DivergedAfterCompletionItemCommittedCommandPending,
            ReasonForUpdate.DivergedAfterReturn, ReasonForUpdate.DivergedAfterTypeChar, ReasonForUpdate.DivergedDueToChangeProposal,
            ReasonForUpdate.DivergedDueToInvalidProposal
        };

        public SuggestionContainer(IEnumerable<Suggestion> suggestions, SuggestionInvocationProperties invocationProperties,
            ICodeWhispererTextView view, ICodeWhispererManager manager, CancellationToken disposalToken)
        {
            _unmodifiedSuggestions = suggestions.ToList();
            _filteredSuggestions = _unmodifiedSuggestions.ToArray();
            _invocationProperties = invocationProperties;
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

            UpdateCurrentProposal();
        }

        public Proposal CurrentProposal { get; private set; }

        public override EditDisplayStyle EditStyle => EditDisplayStyle.GrayText;
        public override TipStyle TipStyle => TipStyle.AlwaysShowTip | TipStyle.TipTopRightJustifiedPlacement;

        public override async Task OnAcceptedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForAccept reason, CancellationToken cancel)
        {
            if (_currentSuggestionIndex >= _filteredSuggestions.Length)
            {
                return;
            }

            var suggestion = _filteredSuggestions[_currentSuggestionIndex];

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

        /// <summary>
        /// Invoked when the proposal's SuggestionSessionBase is dismissed. This occurs when a user rejects a proposal using Escape or by
        /// clicking away from the suggestion focus.
        /// </summary>
        public override Task OnDismissedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForDismiss reason, CancellationToken cancel)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Invoked when a proposal is updated or when a user action diverges from the proposal, causing the proposal
        /// (but not the SuggestionSessionBase) to be dismissed
        /// </summary>
        public override Task OnProposalUpdatedAsync(SuggestionSessionBase session, ProposalBase originalProposal, ProposalBase currentProposal,
            ReasonForUpdate reason, VirtualSnapshotPoint caret, CompletionState completionState, CancellationToken cancel)
        {
            if (_divergenceReasons.Contains(reason))
            {
                ProcessDivergenceAsync(session, reason).LogExceptionAndForget();
            }

            return Task.CompletedTask;
        }

        private async Task ProcessDivergenceAsync(SuggestionSessionBase session,  ReasonForUpdate reason)
        {
            await TaskScheduler.Default;

            // wait for keystroke to surface in the UI
            Thread.Sleep(100);

            if (await FilterSuggestionsAsync(session))
            {
                await session.DisplayProposalAsync(CurrentProposal, _disposalToken);
            }
            else
            {
                await session.DismissAsync(TranslateDismissalReasonFrom(reason), _disposalToken);
            }
        }

        public override async Task OnChangeProposalAsync(
            SuggestionSessionBase session,
            ProposalBase originalProposal, ProposalBase currentProposal,
            bool forward, CancellationToken cancel)
        {
            if (!await FilterSuggestionsAsync(session))
            {
                await session.DismissAsync(ReasonForDismiss.DismissedBySession, _disposalToken);
                return;
            }

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

        /// <summary>
        /// Filters suggestions based on text between the original request's caret position and the current caret position
        /// </summary>
        /// <returns>
        /// Returns true if there are any filtered suggestions to display
        /// </returns>
        public virtual async Task<bool> FilterSuggestionsAsync(SuggestionSessionBase session)
        {
            var currentCaretPosition = _view.GetWpfTextView().GetCaretSnapshotPosition();

            if (_invocationProperties.RequestPosition > currentCaretPosition)
            {
                await session.DismissAsync(ReasonForDismiss.DismissedAfterCaretMoved, _disposalToken);
                return false;
            }

            var prefix = await _view.GetTextBetweenPositionsAsync(_invocationProperties.RequestPosition, currentCaretPosition);

            return FilterSuggestions(prefix);
        }

        public bool FilterSuggestions(string prefix)
        {
            var currentSuggestion = _filteredSuggestions[_currentSuggestionIndex];

            var selectedSuggestions = prefix.Length > 0
                ? _unmodifiedSuggestions.Where(suggestion => suggestion.Text.StartsWith(prefix))
                : _unmodifiedSuggestions;

            _filteredSuggestionTextStartIndex = prefix.Length;

            _filteredSuggestions = selectedSuggestions.ToArray();

            var index = Array.IndexOf(_filteredSuggestions, currentSuggestion);

            _currentSuggestionIndex = CurrentProposal != null && index >= 0
                ? index
                : 0;

            UpdateCurrentProposal();

            return _filteredSuggestions?.Length > 0;
        }

        private void UpdateCurrentProposal()
        {
            CurrentProposal = CreateProposal(_currentSuggestionIndex);
        }

        public override bool HasMultipleSuggestions => _filteredSuggestions.Length > 1;
        public override IReadOnlyList<TipElement> Elements { get; }
#pragma warning disable CS0067 // The event 'SuggestionContainer.PropertyChanged' is never used
        public override event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067 // The event 'SuggestionContainer.PropertyChanged' is never used

        private void NextSuggestion()
        {
            _currentSuggestionIndex = (_currentSuggestionIndex + 1) % _filteredSuggestions.Length;
            UpdateCurrentProposal();
        }

        private void PreviousSuggestion()
        {
            _currentSuggestionIndex = (_currentSuggestionIndex - 1 + _filteredSuggestions.Length) % _filteredSuggestions.Length;
            UpdateCurrentProposal();
        }

        private Proposal CreateProposal(int suggestionIndex)
        {
            if (suggestionIndex >= _filteredSuggestions.Length)
            {
                return null;
            }

            var suggestion = _filteredSuggestions[suggestionIndex];
            var description = CreateProposalDescription(suggestion, suggestionIndex);
            return _view.CreateProposal(suggestion.Text.Substring(_filteredSuggestionTextStartIndex), description);
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
            chunks.Add($"{suggestionIndex + 1} / {_filteredSuggestions.Length}");

            return string.Join(" ", chunks);
        }

        private ReasonForDismiss TranslateDismissalReasonFrom(ReasonForUpdate reason)
        {
            switch (reason)
            {
                case ReasonForUpdate.DivergedAfterBackspace:
                    return ReasonForDismiss.DismissedAfterBackspace;
                case ReasonForUpdate.DivergedAfterCompletionChange:
                    return ReasonForDismiss.DismissedAfterCompletionChange;
                case ReasonForUpdate.DivergedAfterCompletionItemCommitted:
                    return ReasonForDismiss.DismissedAfterCompletionItemCommitted;
                case ReasonForUpdate.DivergedAfterCompletionItemCommittedCommandPending:
                    return ReasonForDismiss.DismissedAfterCompletionItemCommittedCommandPending;
                case ReasonForUpdate.DivergedAfterReturn:
                    return ReasonForDismiss.DismissedAfterReturn;
                case ReasonForUpdate.DivergedAfterTypeChar:
                    return ReasonForDismiss.DismissedAfterTypeChar;
                case ReasonForUpdate.DivergedDueToInvalidProposal:
                    return ReasonForDismiss.DismissedDueToInvalidProposal;
                default:
                    return ReasonForDismiss.DismissedBySession;
            }
        }
    }
}
