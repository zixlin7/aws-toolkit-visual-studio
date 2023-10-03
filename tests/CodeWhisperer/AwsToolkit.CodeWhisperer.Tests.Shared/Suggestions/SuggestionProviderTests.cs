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

using Microsoft.VisualStudio.Project;

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

        public SuggestionProviderTests()
        {
            _sut = new SuggestionProvider(_lspClient, _settingsRepository,
                _toolkitContextFixture.ToolkitContextProvider);
        }

        [Fact]
        public async Task PauseAutoSuggestAsync()
        {
            _settingsRepository.Settings.PauseAutomaticSuggestions = false;

            await _sut.PauseAutoSuggestAsync();
            (await _sut.IsAutoSuggestPausedAsync()).Should().BeTrue();
            _settingsRepository.Settings.PauseAutomaticSuggestions.Should().BeTrue();
        }

        [Fact]
        public async Task ResumeAutoSuggestAsync()
        {
            _settingsRepository.Settings.PauseAutomaticSuggestions = true;

            await _sut.ResumeAutoSuggestAsync();

            (await _sut.IsAutoSuggestPausedAsync()).Should().BeFalse();
            _settingsRepository.Settings.PauseAutomaticSuggestions.Should().BeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task IsAutoSuggestPausedAsync(bool isPaused)
        {
            _settingsRepository.Settings.PauseAutomaticSuggestions = isPaused;

            (await _sut.IsAutoSuggestPausedAsync()).Should().Be(isPaused);
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

            var suggestions = (await _sut.GetSuggestionsAsync(request)).ToList();

            suggestions.Should().HaveCount(sampleCompletions);
            foreach (var suggestion in suggestions)
            {
                var expectedCompletion =
                    _lspClient.InlineCompletions.InlineCompletions.Items.FirstOrDefault(x =>
                        x.InsertText == suggestion.Text);
                expectedCompletion.Should().NotBeNull();

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

            return new InlineCompletionList() { Items = completions.ToArray(), };
        }

        private InlineCompletionItem CreateSampleCompletionItem(string text)
        {
            return new InlineCompletionItem()
            {
                InsertText = text,
                References = new[]
                {
                    CreateSampleInlineCompletionReference(),
                }
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
