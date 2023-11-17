using System;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
using Amazon.AWSToolkit.Models.Text;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;
using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class SuggestionProviderTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();

        private readonly FakeCodeWhispererClient _lspClient = new FakeCodeWhispererClient();
        private readonly SuggestionProvider _sut;
        private readonly string _sampleSessionId = "sample-sessionId";

        public SuggestionProviderTests()
        {
            _sut = new SuggestionProvider(_lspClient, _settingsRepository,
                _toolkitContextFixture.ToolkitContextProvider);
        }

        [Fact]
        public async Task PauseAutoSuggestAsync()
        {
            _settingsRepository.Settings.AutomaticSuggestionsEnabled = true;

            await _sut.PauseAutoSuggestAsync();
            (await _sut.IsAutoSuggestPausedAsync()).Should().BeTrue();
            _settingsRepository.Settings.AutomaticSuggestionsEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task ResumeAutoSuggestAsync()
        {
            _settingsRepository.Settings.AutomaticSuggestionsEnabled = false;

            await _sut.ResumeAutoSuggestAsync();

            (await _sut.IsAutoSuggestPausedAsync()).Should().BeFalse();
            _settingsRepository.Settings.AutomaticSuggestionsEnabled.Should().BeTrue();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAutoSuggestPausedAsync(bool isEnabled)
        {
            _settingsRepository.Settings.AutomaticSuggestionsEnabled = isEnabled;

            (await _sut.IsAutoSuggestPausedAsync()).Should().NotBe(isEnabled);
        }

        [Fact]
        public async Task GetSuggestionsAsync()
        {
            var sampleCompletions = 3;
            _lspClient.InlineCompletions.InlineCompletions = CreateInlineCompletionList(sampleCompletions);

            var request = new GetSuggestionsRequest()
            {
                FilePath = @"c:\sample\file.cs",
                CursorPosition = new Position(0, 0),
                IsAutoSuggestion = false,
            };

            var suggestionSession = await _sut.GetSuggestionsAsync(request);

            suggestionSession.SessionId.Should().BeEquivalentTo(_sampleSessionId);
            suggestionSession.Suggestions.Should().HaveCount(sampleCompletions);
            suggestionSession.RequestedAtEpoch.Should().BeGreaterThan(default);

            foreach (var suggestion in suggestionSession.Suggestions)
            {
                var expectedCompletion =
                    _lspClient.InlineCompletions.InlineCompletions.Items.FirstOrDefault(x =>
                        x.InsertText == suggestion.Text);
                expectedCompletion.Should().NotBeNull();

                suggestion.Id.Should().BeEquivalentTo(expectedCompletion.ItemId);
                suggestion.References.Should().HaveCount(expectedCompletion.References.Length);
                foreach (var expectedReference in expectedCompletion.References)
                {
                    suggestion.References.Should().Contain(actualReference => IsMatch(expectedReference, actualReference));
                }
            }
        }

        private bool IsMatch(InlineCompletionReference expectedReference, SuggestionReference actualReference)
        {
            return expectedReference.ReferenceName == actualReference.Name
                && expectedReference.ReferenceUrl == actualReference.Url
                && expectedReference.LicenseName == actualReference.LicenseName
                && expectedReference.Position.StartCharacter == actualReference.StartIndex
                && expectedReference.Position.EndCharacter == actualReference.EndIndex;
        }

        private InlineCompletionList CreateInlineCompletionList(int sampleCompletions)
        {
            var completions = Enumerable.Range(0, sampleCompletions).Select(i => CreateSampleCompletionItem($"Sample Suggestion {i}"));

            return new InlineCompletionList() { Items = completions.ToArray(), SessionId = _sampleSessionId };
        }

        private InlineCompletionItem CreateSampleCompletionItem(string text)
        {
            return new InlineCompletionItem()
            {
                InsertText = text,
                References = new[]
                {
                    CreateSampleInlineCompletionReference(),
                },
                ItemId = Guid.NewGuid().ToString()
            };
        }

        private InlineCompletionReference CreateSampleInlineCompletionReference()
        {
            var id = Guid.NewGuid().ToString();
            return new InlineCompletionReference()
            {
                ReferenceName = $"reference-{id}",
                ReferenceUrl = $"url-{id}",
                LicenseName = $"license-{id}",
                Position = new ReferencePosition() { StartCharacter = 0, EndCharacter = 0, },
            };
        }
    }
}
