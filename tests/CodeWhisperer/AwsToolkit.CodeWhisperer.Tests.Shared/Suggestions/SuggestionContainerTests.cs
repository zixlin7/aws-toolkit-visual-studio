using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Documents;

using FluentAssertions;

using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;

using Moq;

using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class SuggestionContainerTests
    {
        private readonly FakeCodeWhispererTextView _textView = new FakeCodeWhispererTextView();
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly Mock<SuggestionSessionBase> _suggestionSession = new Mock<SuggestionSessionBase>();
        private readonly SuggestionContainer _sut;

        private static class SampleSuggestions
        {
            public static readonly Suggestion NoReferences = SampleSuggestionProducer.CreateSampleSuggestion("a", 0);
            public static readonly Suggestion OneReference = SampleSuggestionProducer.CreateSampleSuggestion("b", 1);
            public static readonly Suggestion ThreeReferences = SampleSuggestionProducer.CreateSampleSuggestion("c", 3);

            public static readonly Suggestion[] AllSuggestions = new Suggestion[]
            {
                NoReferences, OneReference, ThreeReferences,
            };
        }

        public SuggestionContainerTests()
        {
            _textView.FilePath = "some-path.code";
            _sut = new SuggestionContainer(SampleSuggestions.AllSuggestions, _textView, _manager, CancellationToken.None);
        }

        [Fact]
        public void HasInitialProposal()
        {
            // Exercises making a proposal without any references
            AssertProposalText(SampleSuggestions.NoReferences, _sut.CurrentProposal);
            _sut.CurrentProposal.Description.Should().BeEquivalentTo("Suggestion 1 / 3");
        }

        [Fact]
        public async Task AcceptSuggestionWithoutReference()
        {
            await AcceptCurrentProposalAsync();

            _manager.LoggedReferences.Should().BeEmpty();
        }

        [Fact]
        public async Task AdvanceLoopsBackToFirstProposal()
        {
            // Exercises wrapping around the end boundary, back to the start of the suggestion list
            for (var i = 0; i < SampleSuggestions.AllSuggestions.Length; i++)
            {
                await CycleToNextProposalAsync();
            }

            AssertProposalText(SampleSuggestions.NoReferences, _sut.CurrentProposal);
        }

        [Fact]
        public async Task AdvancesToSecondProposal()
        {
            // Exercises making a proposal with one reference
            var expectedSuggestion = SampleSuggestions.OneReference;

            await CycleToNextProposalAsync();

            AssertProposalText(expectedSuggestion, _sut.CurrentProposal);
            _sut.CurrentProposal.Description.Should().BeEquivalentTo($"Suggestion (License: {expectedSuggestion.References.First().LicenseName}) 2 / 3");
        }

        [Fact]
        public async Task AcceptSuggestionWithReference()
        {
            await CycleToNextProposalAsync();
            await AcceptCurrentProposalAsync();

            _manager.LoggedReferences.Should().HaveCount(1);

            var loggedReference = _manager.LoggedReferences.First();
            loggedReference.Suggestion.Should().Be(SampleSuggestions.OneReference);
            loggedReference.SuggestionReference.Should().Be(SampleSuggestions.OneReference.References.First());
            loggedReference.Filename.Should().BeEquivalentTo(_textView.FilePath);
        }

        [Fact]
        public async Task ReversesToThirdProposal()
        {
            // Exercises making a proposal with three reference
            // Also exercises wrapping around the start boundary, back to the end of the suggestion list
            var expectedSuggestion = SampleSuggestions.ThreeReferences;

            await CycleToPreviousProposalAsync();

            AssertProposalText(expectedSuggestion, _sut.CurrentProposal);
            _sut.CurrentProposal.Description.Should().BeEquivalentTo($"Suggestion (Licenses: license-c-0, license-c-1, license-c-2) 3 / 3");
        }

        [Fact]
        public async Task AcceptSuggestionWithMultipleReference()
        {
            await CycleToPreviousProposalAsync();
            await AcceptCurrentProposalAsync();

            var expectedSuggestion = SampleSuggestions.ThreeReferences;

            _manager.LoggedReferences.Should().HaveCount(expectedSuggestion.References.Count);

            _manager.LoggedReferences.Should().AllSatisfy(referenceRequest =>
            {
                referenceRequest.Suggestion.Should().BeEquivalentTo(expectedSuggestion);
                referenceRequest.Filename.Should().BeEquivalentTo(_textView.FilePath);
            });
            _manager.LoggedReferences[0].SuggestionReference.Should().BeEquivalentTo(expectedSuggestion.References[0]);
            _manager.LoggedReferences[1].SuggestionReference.Should().BeEquivalentTo(expectedSuggestion.References[1]);
            _manager.LoggedReferences[2].SuggestionReference.Should().BeEquivalentTo(expectedSuggestion.References[2]);
        }

        private async Task CycleToNextProposalAsync()
        {
            await _sut.OnChangeProposalAsync(_suggestionSession.Object, _sut.CurrentProposal, _sut.CurrentProposal, true,
                CancellationToken.None);
        }

        private async Task CycleToPreviousProposalAsync()
        {
            await _sut.OnChangeProposalAsync(_suggestionSession.Object, _sut.CurrentProposal, _sut.CurrentProposal,
                false, CancellationToken.None);
        }

        private async Task AcceptCurrentProposalAsync()
        {
            await _sut.OnAcceptedAsync(_suggestionSession.Object,
                _sut.CurrentProposal, _sut.CurrentProposal,
                ReasonForAccept.AcceptedByCommand, CancellationToken.None);
        }

        private void AssertProposalText(Suggestion expectedSuggestion, Proposal proposal)
        {
            var edit = proposal.Edits.First();
            edit.ReplacementText.Should().BeEquivalentTo(expectedSuggestion.Text);
        }
    }
}
