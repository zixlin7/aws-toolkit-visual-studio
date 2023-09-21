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
            Func<Task> operation = () => _sut.PauseAutoSuggestAsync();

            await operation.Should().ThrowAsync<NotImplementedException>();
        }

        [Fact]
        public async Task ResumeAutoSuggestAsync()
        {
            Func<Task> operation = () => _sut.ResumeAutoSuggestAsync();

            await operation.Should().ThrowAsync<NotImplementedException>();
        }

        [Fact]
        public async Task IsAutoSuggestPausedAsync()
        {
            Func<Task<bool>> operation = () => _sut.IsAutoSuggestPausedAsync();

            await operation.Should().ThrowAsync<NotImplementedException>();
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
