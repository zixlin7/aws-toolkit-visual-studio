using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests
{
    public class CodeWhispererManagerTests
    {
        private readonly FakeConnection _connection = new FakeConnection();
        private readonly FakeSuggestionProvider _suggestionProvider = new FakeSuggestionProvider();
        private readonly CodeWhispererManager _sut;

        public CodeWhispererManagerTests()
        {
            _sut = new CodeWhispererManager(_connection, _suggestionProvider);
        }

        [Fact]
        public async Task SignInAsync()
        {
            _connection.ConnectionStatus = ConnectionStatus.Disconnected;

            await _sut.SignInAsync();

            _sut.GetStatus().Should().Be(ConnectionStatus.Connected);
        }

        [Fact]
        public async Task SignOutAsync()
        {
            _connection.ConnectionStatus = ConnectionStatus.Connected;

            await _sut.SignOutAsync();

            _sut.GetStatus().Should().Be(ConnectionStatus.Disconnected);
        }

        [Theory]
        [InlineData(ConnectionStatus.Connected)]
        [InlineData(ConnectionStatus.Expired)]
        [InlineData(ConnectionStatus.Disconnected)]
        public void GetStatus(ConnectionStatus expectedStatus)
        {
            _connection.ConnectionStatus = expectedStatus;

            _sut.GetStatus().Should().Be(expectedStatus);
        }

        [Fact]
        public void StatusChanged()
        {
            _connection.ConnectionStatus = ConnectionStatus.Disconnected;

            var eventArgs = Assert.Raises<ConnectionStatusChangedEventArgs>(
                attach => _sut.StatusChanged += attach,
                detach => _sut.StatusChanged -= detach,
                () =>
                {
                    _connection.ConnectionStatus = ConnectionStatus.Connected;
                    _connection.RaiseStatusChanged();
                });

            eventArgs.Arguments.ConnectionStatus.Should().Be(ConnectionStatus.Connected);
        }

        [Fact]
        public async Task PauseAutoSuggestAsync()
        {
            _suggestionProvider.PauseAutomaticSuggestions = false;

            await _sut.PauseAutoSuggestAsync();

            (await _sut.IsAutoSuggestPausedAsync()).Should().BeTrue();
        }

        [Fact]
        public async Task ResumeAutoSuggestAsync()
        {
            _suggestionProvider.PauseAutomaticSuggestions = true;

            await _sut.ResumeAutoSuggestAsync();

            (await _sut.IsAutoSuggestPausedAsync()).Should().BeFalse();
        }

        [Fact]
        public async Task GetSuggestionsAsync()
        {
            _suggestionProvider.Suggestions.Add(new Suggestion());

            var suggestions = await _sut.GetSuggestionsAsync();

            suggestions.Should().BeEquivalentTo(_suggestionProvider.Suggestions);
        }

        [Fact]
        public async Task GetSuggestionsAsync_WhenPaused()
        {
            _suggestionProvider.Suggestions.Add(new Suggestion());

            await _sut.PauseAutoSuggestAsync();
            var suggestions = await _sut.GetSuggestionsAsync();

            suggestions.Should().BeEmpty();
        }
    }
}
