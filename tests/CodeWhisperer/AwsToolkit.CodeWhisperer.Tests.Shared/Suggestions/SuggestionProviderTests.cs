using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Settings;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Moq;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class SuggestionProviderTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly FakeCodeWhispererSettingsRepository _settingsRepository =
            new FakeCodeWhispererSettingsRepository();

        private readonly Mock<IToolkitLspClient> _lspClient = new Mock<IToolkitLspClient>();
        private readonly SuggestionProvider _sut;

        public SuggestionProviderTests()
        {
            _sut = new SuggestionProvider(_lspClient.Object, _settingsRepository,
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
            // todo : mock or fake the CWSPR language client

            var suggestions = await _sut.GetSuggestionsAsync();

            suggestions.Should().BeEmpty();
        }
    }
}
