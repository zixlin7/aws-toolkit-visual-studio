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
            for (var i = 0; i < sampleCompletions; i++)
            {
                suggestions.Should().Contain(suggestion => suggestion.Text == $"Sample Suggestion {i}");
            }
        }

        private InlineCompletionList CreateInlineCompletionList(int sampleCompletions)
        {
            var completions = Enumerable.Range(0, sampleCompletions).Select(i => CreateSampleCompletionItem($"Sample Suggestion {i}"));

            return new InlineCompletionList() { Items = completions.ToArray(), };
        }

        private InlineCompletionItem CreateSampleCompletionItem(string text)
        {
            return new InlineCompletionItem() { InsertText = text, };
        }
    }
}
