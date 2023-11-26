using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Documents;
using Amazon.AWSToolkit.Util;

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
        private readonly StubSuggestionContainer _sut;
        private readonly StubSuggestionContainer _textSuggestionContainer;
        private readonly SuggestionInvocationProperties _invocationProperties = new SuggestionInvocationProperties() {SessionId = "sample-sessionId", RequestedAtEpoch = DateTime.Now.AsUnixMilliseconds()};

        private static class SampleSuggestions
        {
            public static readonly Suggestion NoReferences = SampleSuggestionProducer.CreateSampleSuggestion("a", 0);
            public static readonly Suggestion OneReference = SampleSuggestionProducer.CreateSampleSuggestion("b", 1);
            public static readonly Suggestion ThreeReferences = SampleSuggestionProducer.CreateSampleSuggestion("c", 3);

            public static readonly Suggestion FibonacciSuggestion =
                SampleSuggestionProducer.CreateSampleSuggestion("id-1", "if (n <= 1)\r\n{\r\n    return n;\r\n}\r\n\r\nreturn CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            public static readonly Suggestion FibonacciSuggestion2 =
                SampleSuggestionProducer.CreateSampleSuggestion("id-2", "if (n == 0 || n == 1)\r\n{\r\n    return n;\r\n}\r\n\r\nreturn CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            public static readonly Suggestion FibonacciSuggestion3 =
                SampleSuggestionProducer.CreateSampleSuggestion("id-3", "if (n == 0)\r\n{\r\n    return 0;\r\n}\r\n\r\nif (n == 1)\r\n{\r\n    return 1;\r\n}\r\n\r\nreturn CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            public static readonly Suggestion FibonacciSuggestion4 =
                SampleSuggestionProducer.CreateSampleSuggestion("id-4", "if (n == 0)\r\n{\r\n    return 0;\r\n}\r\nelse if (n == 1)\r\n{\r\n    return 1;\r\n}\r\nelse\r\n{\r\n    return CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            public static readonly Suggestion EmptySuggestion = SampleSuggestionProducer.CreateSampleSuggestion("id-5", " ");

            public static readonly Suggestion[] ReferenceSuggestions = new Suggestion[]
            {
                NoReferences, OneReference, ThreeReferences
            };

            public static readonly Suggestion[] TextSuggestions = new Suggestion[]
            {
                FibonacciSuggestion, FibonacciSuggestion2, FibonacciSuggestion3, FibonacciSuggestion4, EmptySuggestion
            };
        }

        internal class StubSuggestionContainer : SuggestionContainer
        {
            public StubSuggestionContainer(Suggestion[] suggestions, SuggestionInvocationProperties properties,
                FakeCodeWhispererTextView textView, FakeCodeWhispererManager manager, CancellationToken cancellationToken)
                : base(suggestions, properties, textView, manager, cancellationToken)
            {
            }

            public override Task<bool> FilterSuggestionsAsync(SuggestionSessionBase suggestionSession)
            {
                return Task.FromResult(true);
            }
        }

        public SuggestionContainerTests()
        {
            _textView.FilePath = "some-path.code";
            _sut = new StubSuggestionContainer(SampleSuggestions.ReferenceSuggestions, _invocationProperties, _textView, _manager, CancellationToken.None);
            _textSuggestionContainer = new StubSuggestionContainer(SampleSuggestions.TextSuggestions, _invocationProperties, _textView, _manager, CancellationToken.None);
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
        public async Task VerifySuggestionSessionResult_WhenFirstSuggestionSeenAndAccepted()
        {
            // introduce a minor delay to ensure some latency time is observed with the tests
            await Task.Delay(5);
            var expectedAcceptedSuggestion = SampleSuggestions.ReferenceSuggestions.First();

            _sut.OnProposalDisplayed(_sut.CurrentProposal.ProposalId);

            // introduce a minor delay to ensure some total session time is observed with the tests
            await Task.Delay(5);
            await AcceptCurrentProposalAsync();

            var actualAcceptedSuggestion = GetAcceptedSuggestionCompletionResults().Single();
            var seenSuggestions = GetSeenSuggestionCompletionResults();

            actualAcceptedSuggestion.Key.Should().BeEquivalentTo(expectedAcceptedSuggestion.Id);
            seenSuggestions.Count().Should().Be(1);
            seenSuggestions.First().Key.Should().BeEquivalentTo(expectedAcceptedSuggestion.Id);

            AssertValidSeenCompletionResultParams();
        }


        [Fact]
        public async Task VerifySuggestionSessionResult_WhenFirstSuggestionSeenAndDismissed()
        {
            // introduce a minor delay to ensure some latency time is observed with the tests
            await Task.Delay(5);

            var expectedSeenSuggestion = SampleSuggestions.ReferenceSuggestions.First();

            _sut.OnProposalDisplayed(_sut.CurrentProposal.ProposalId);

            // introduce a minor delay to ensure some total session time is observed with the tests
            await Task.Delay(5);

            await DismissCurrentProposalAsync(_sut);

            var seenSuggestions = GetSeenSuggestionCompletionResults();
            var acceptedSuggestions = GetAcceptedSuggestionCompletionResults();

            acceptedSuggestions.Should().BeEmpty();
            seenSuggestions.Count().Should().Be(1);
            seenSuggestions.First().Key.Should().BeEquivalentTo(expectedSeenSuggestion.Id);

            AssertValidSeenCompletionResultParams();
        }


        [Fact]
        public async Task VerifySuggestionSessionResult_WhenMultipleSuggestionsSeenAndDismissed()
        {
            SetupContainerDisplay(_sut);
            // introduce a minor delay to ensure some latency time is observed with the tests
            await Task.Delay(5);

            // Exercises wrapping around the end boundary, back to the start of the suggestion list to see all
            for (var i = 0; i < SampleSuggestions.ReferenceSuggestions.Length; i++)
            {
                await CycleToNextProposalAsync();
            }
            // introduce a minor delay to ensure some total session time is observed with the tests
            await Task.Delay(5);
            await DismissCurrentProposalAsync(_sut);

            var seenSuggestions = GetSeenSuggestionCompletionResults();
            var acceptedSuggestions = GetAcceptedSuggestionCompletionResults();

            acceptedSuggestions.Should().BeEmpty();
            seenSuggestions.Count().Should().Be(SampleSuggestions.ReferenceSuggestions.Length);

            AssertValidSeenCompletionResultParams();
        }

        [Fact]
        public async Task VerifySuggestionSessionResultMarksDiscarded_WhenSuggestionsFilteredAndDismissed()
        {
            // introduce a minor delay to ensure some latency time is observed with the tests
            await Task.Delay(5);

            var expectedDiscardedSuggestionList = new List<string>()
            {
                SampleSuggestions.FibonacciSuggestion.Id, SampleSuggestions.EmptySuggestion.Id
            };

            // shows FibonacciSuggestion
            _textSuggestionContainer.OnProposalDisplayed(_textSuggestionContainer.CurrentProposal.ProposalId);

            var prefix = "if (n == 0";
            _textSuggestionContainer.FilterSuggestions(prefix);

            // see first in filtered list
            _textSuggestionContainer.OnProposalDisplayed(_textSuggestionContainer.CurrentProposal.ProposalId);

            // introduce a minor delay to ensure some total session time is observed with the tests
            await Task.Delay(30);

            await DismissCurrentProposalAsync(_textSuggestionContainer);

            var seenSuggestions = GetSeenSuggestionCompletionResults();
            var discardedSuggestions = GetDiscardedSuggestionCompletionResults().ToList();
            var acceptedSuggestions = GetAcceptedSuggestionCompletionResults();

            // verify none accepted
            acceptedSuggestions.Should().BeEmpty();

            // verify single suggestion seen and is not the one that was seen previously and has been discarded
            seenSuggestions.Count().Should().Be(1);
            seenSuggestions.First().Key.Should().NotBe(SampleSuggestions.FibonacciSuggestion.Id);

            // verify discarded list matches expectation
            var discardedSuggestionIdList = discardedSuggestions.Select(x => x.Key);
            discardedSuggestionIdList.Should().BeEquivalentTo(expectedDiscardedSuggestionList);

            AssertValidSeenCompletionResultParams();
        }

        [Fact]
        public async Task VerifySuggestionSessionResultResetsDiscardedWithBacktracking_WhenSuggestionFilteredAndDismissed()
        {
            // introduce a minor delay to ensure some latency time is observed with the tests
            await Task.Delay(5);

            // filter Fibonacci Suggestion so that it is initially marked discarded
            var prefix = "if (n == 0";
            _textSuggestionContainer.FilterSuggestions(prefix);

            // backtrack to add back Fibonacci Suggestion
            prefix = "i";
            _textSuggestionContainer.FilterSuggestions(prefix);

            var expectedDiscardedSuggestionList = new List<string>()
            {
                SampleSuggestions.EmptySuggestion.Id
            };

            // see first in filtered list
            _textSuggestionContainer.OnProposalDisplayed(_textSuggestionContainer.CurrentProposal.ProposalId);

            // introduce a minor delay to ensure some total session time is observed with the tests
            await Task.Delay(5);

            await DismissCurrentProposalAsync(_textSuggestionContainer);

            var seenSuggestions = GetSeenSuggestionCompletionResults();
            var discardedSuggestions = GetDiscardedSuggestionCompletionResults().ToList();
            var acceptedSuggestions = GetAcceptedSuggestionCompletionResults();

            // verify none accepted
            acceptedSuggestions.Should().BeEmpty();

            // verify discarded list matches expectation
            var discardedSuggestionIdList = discardedSuggestions.Select(x => x.Key);
            discardedSuggestionIdList.Should().BeEquivalentTo(expectedDiscardedSuggestionList);

            // verify single suggestion seen and is the one that was previously has been discarded
            seenSuggestions.Count().Should().Be(1);
            seenSuggestions.First().Key.Should().NotBe(SampleSuggestions.FibonacciSuggestion.Id);


            AssertValidSeenCompletionResultParams();
        }


        [Fact]
        public async Task AdvanceLoopsBackToFirstProposal()
        {
            // Exercises wrapping around the end boundary, back to the start of the suggestion list
            for (var i = 0; i < SampleSuggestions.ReferenceSuggestions.Length; i++)
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

        private async Task DismissCurrentProposalAsync(SuggestionContainer container)
        {
            await container.OnDismissedAsync(_suggestionSession.Object,
                container.CurrentProposal, container.CurrentProposal,
                ReasonForDismiss.DismissedAfterReturn, CancellationToken.None);
        }

        private void SetupContainerDisplay(SuggestionContainer container)
        {
            _suggestionSession
                .Setup(x => x.DisplayProposalAsync(It.IsAny<ProposalBase>(), It.IsAny<CancellationToken>()))
                .Callback((ProposalBase baseProposal, CancellationToken token) => container.OnProposalDisplayed(baseProposal.ProposalId))
                .Returns(Task.CompletedTask);
        }

        private IEnumerable<KeyValuePair<string, InlineCompletionStates>> GetSeenSuggestionCompletionResults()
        {
            return _manager.SessionResultsParam.CompletionSessionResult.Where(x => x.Value.Seen);
        }

        private IEnumerable<KeyValuePair<string, InlineCompletionStates>> GetDiscardedSuggestionCompletionResults()
        {
            return _manager.SessionResultsParam.CompletionSessionResult.Where(x => x.Value.Discarded);
        }

        private IEnumerable<KeyValuePair<string, InlineCompletionStates>> GetAcceptedSuggestionCompletionResults()
        {
            return _manager.SessionResultsParam.CompletionSessionResult.Where(x => x.Value.Accepted);
        }

        private void AssertValidSeenCompletionResultParams()
        {
            // assert durations were recorded
            _manager.SessionResultsParam.FirstCompletionDisplayLatency.Should().BeGreaterThan(2);
            _manager.SessionResultsParam.TotalSessionDisplayTime.Should().BeGreaterThan(2);
            _manager.SessionResultsParam.SessionId.Should().BeEquivalentTo(_invocationProperties.SessionId);
        }

        private void AssertProposalText(Suggestion expectedSuggestion, Proposal proposal)
        {
            var edit = proposal.Edits.First();
            edit.ReplacementText.Should().BeEquivalentTo(expectedSuggestion.Text);
        }

        [Fact]
        private void FilterSuggestions_NoPrefixFiltersOutWhitespace()
        {
            _textSuggestionContainer.FilterSuggestions(string.Empty);

            _textSuggestionContainer.CurrentProposal.Description
                .Should().Be(CreateExpectedDescriptionText(0, SampleSuggestions.TextSuggestions.Length-1));

            _textSuggestionContainer.CurrentProposal.Edits[0].ReplacementText
                .Should().Be(SampleSuggestions.TextSuggestions[0].Text);
        }

        [Fact]
        private void FilterSuggestions_PrefixMatchesAllNonWhitespaceSuggestions()
        {
            var prefix = "if (n ";

            _textSuggestionContainer.FilterSuggestions(prefix);

            _textSuggestionContainer.CurrentProposal.Description
                .Should().Be(CreateExpectedDescriptionText(0, SampleSuggestions.TextSuggestions.Length-1));

            _textSuggestionContainer.CurrentProposal.Edits[0].ReplacementText
                .Should().Be(SampleSuggestions.TextSuggestions[0].Text.Substring(prefix.Length));
        }

        [Theory]
        [InlineData("if (n =", 3)]
        [InlineData("if (n == 0)", 2)]
        [InlineData("if (n == 0)\r\n{\r\n    return 0;\r\n}\r\ne", 1)]
        private void FilterSuggestions_PrefixMatchesSomeSuggestions(string prefix, int expectedFilteredSuggestionCount)
        {
            _textSuggestionContainer.FilterSuggestions(prefix);

            _textSuggestionContainer.CurrentProposal.Description.Should().Be(CreateExpectedDescriptionText(0, expectedFilteredSuggestionCount));

            // returns -1 if an index isn't found that matches the condition
            Array.FindIndex(SampleSuggestions.TextSuggestions,
                    s => s.Text.Substring(prefix.Length).Equals(_textSuggestionContainer.CurrentProposal.Edits[0].ReplacementText))
                .Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        private void FilterSuggestions_PrefixMatchesNoSuggestions()
        {
            var prefix = "if (n >";

            _textSuggestionContainer.FilterSuggestions(prefix);

            _textSuggestionContainer.CurrentProposal.Should().Be(null);
        }

        [Fact]
        private void FilterSuggestions_BackspaceResurfacesFilteredSuggestions()
        {
            var firstFilterSuggestionCount = 2;
            var secondFilterSuggestionCount = 3;

            var prefix = "if (n == 0)";

            _textSuggestionContainer.FilterSuggestions(prefix);

            _textSuggestionContainer.CurrentProposal.Description.Should()
                .Be(CreateExpectedDescriptionText(0, firstFilterSuggestionCount));

            prefix = "if (n == 0";

            _textSuggestionContainer.FilterSuggestions(prefix);

            _textSuggestionContainer.CurrentProposal.Description.Should()
                .Be(CreateExpectedDescriptionText(1, secondFilterSuggestionCount));
        }

        private string CreateExpectedDescriptionText(int index, int length)
        {
            return $"Suggestion {index + 1} / {length}";
        }
    }
}
