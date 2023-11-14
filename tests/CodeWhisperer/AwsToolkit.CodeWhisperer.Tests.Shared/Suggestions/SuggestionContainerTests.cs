using System;
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
        private readonly StubSuggestionContainer _sut;
        private readonly SuggestionContainer _textSuggestionContainer;

        private static class SampleSuggestions
        {
            public static readonly Suggestion NoReferences = SampleSuggestionProducer.CreateSampleSuggestion("a", 0);
            public static readonly Suggestion OneReference = SampleSuggestionProducer.CreateSampleSuggestion("b", 1);
            public static readonly Suggestion ThreeReferences = SampleSuggestionProducer.CreateSampleSuggestion("c", 3);

            private static readonly Suggestion _fibonacciSuggestion =
                SampleSuggestionProducer.CreateSampleSuggestion("if (n <= 1)\r\n{\r\n    return n;\r\n}\r\n\r\nreturn CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            private static readonly Suggestion _fibonacciSuggestion2 =
                SampleSuggestionProducer.CreateSampleSuggestion("if (n == 0 || n == 1)\r\n{\r\n    return n;\r\n}\r\n\r\nreturn CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            private static readonly Suggestion _fibonacciSuggestion3 =
                SampleSuggestionProducer.CreateSampleSuggestion("if (n == 0)\r\n{\r\n    return 0;\r\n}\r\n\r\nif (n == 1)\r\n{\r\n    return 1;\r\n}\r\n\r\nreturn CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            private static readonly Suggestion _fibonacciSuggestion4 =
                SampleSuggestionProducer.CreateSampleSuggestion("if (n == 0)\r\n{\r\n    return 0;\r\n}\r\nelse if (n == 1)\r\n{\r\n    return 1;\r\n}\r\nelse\r\n{\r\n    return CalculateFibonacci(n - 1) + CalculateFibonacci(n - 2);");

            private static readonly Suggestion _emptySuggestion = SampleSuggestionProducer.CreateSampleSuggestion("    ");

            public static readonly Suggestion[] ReferenceSuggestions = new Suggestion[]
            {
                NoReferences, OneReference, ThreeReferences
            };

            public static readonly Suggestion[] TextSuggestions = new Suggestion[]
            {
                _fibonacciSuggestion, _fibonacciSuggestion2, _fibonacciSuggestion3, _fibonacciSuggestion4, _emptySuggestion
            };
        }

        internal class StubSuggestionContainer : SuggestionContainer
        {
            public StubSuggestionContainer(Suggestion[] suggestions, SuggestionInvocationProperties properties, FakeCodeWhispererTextView textView, FakeCodeWhispererManager  manager, CancellationToken cancellationToken)
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
            _sut = new StubSuggestionContainer(SampleSuggestions.ReferenceSuggestions, new SuggestionInvocationProperties(), _textView, _manager, CancellationToken.None);
            _textSuggestionContainer = new SuggestionContainer(SampleSuggestions.TextSuggestions, new SuggestionInvocationProperties(), _textView, _manager, CancellationToken.None);
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
