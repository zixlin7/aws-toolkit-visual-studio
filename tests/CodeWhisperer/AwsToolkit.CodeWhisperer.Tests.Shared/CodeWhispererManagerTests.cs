using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests
{
    [Collection(VsMockCollection.CollectionName)]
    public class CodeWhispererManagerTests
    {
        private readonly FakeConnection _connection = new FakeConnection();
        private readonly FakeSuggestionProvider _suggestionProvider = new FakeSuggestionProvider();
        private readonly FakeReferenceLogger _referenceLogger = new FakeReferenceLogger();
        private readonly CodeWhispererManager _sut;

        public CodeWhispererManagerTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new CodeWhispererManager(_connection, _suggestionProvider, _referenceLogger, taskFactoryProvider);
        }

        [Fact]
        public async Task SignInAsync()
        {
            _connection.Status = ConnectionStatus.Disconnected;

            await _sut.SignInAsync();

            _sut.ConnectionStatus.Should().Be(ConnectionStatus.Connected);
        }

        [Fact]
        public async Task SignOutAsync()
        {
            _connection.Status = ConnectionStatus.Connected;

            await _sut.SignOutAsync();

            _sut.ConnectionStatus.Should().Be(ConnectionStatus.Disconnected);
        }

        [Theory]
        [InlineData(ConnectionStatus.Connected)]
        [InlineData(ConnectionStatus.Expired)]
        [InlineData(ConnectionStatus.Disconnected)]
        public void GetStatus(ConnectionStatus expectedStatus)
        {
            _connection.Status = expectedStatus;

            _sut.ConnectionStatus.Should().Be(expectedStatus);
        }

        [Fact]
        public void StatusChanged()
        {
            _connection.Status = ConnectionStatus.Disconnected;

            var eventArgs = Assert.Raises<ConnectionStatusChangedEventArgs>(
                attach => _sut.ConnectionStatusChanged += attach,
                detach => _sut.ConnectionStatusChanged -= detach,
                () =>
                {
                    _connection.Status = ConnectionStatus.Connected;
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
#pragma warning disable VSTHRD103
            // ReSharper disable once MethodHasAsyncOverload
            _sut.IsAutoSuggestPaused().Should().BeTrue();
#pragma warning restore VSTHRD103
        }

        [Fact]
        public async Task ResumeAutoSuggestAsync()
        {
            _suggestionProvider.PauseAutomaticSuggestions = true;

            await _sut.ResumeAutoSuggestAsync();

            (await _sut.IsAutoSuggestPausedAsync()).Should().BeFalse();
#pragma warning disable VSTHRD103
            // ReSharper disable once MethodHasAsyncOverload
            _sut.IsAutoSuggestPaused().Should().BeFalse();
#pragma warning restore VSTHRD103
        }

        [Fact]
        public async Task GetSuggestionsAsync()
        {
            _suggestionProvider.Suggestions.Add(new Suggestion());

            var suggestions = await _sut.GetSuggestionsAsync(new GetSuggestionsRequest());

            suggestions.Should().BeEquivalentTo(_suggestionProvider.Suggestions);
        }

        [Fact]
        public async Task GetSuggestionsAsync_WhenPaused()
        {
            _suggestionProvider.Suggestions.Add(new Suggestion());

            await _sut.PauseAutoSuggestAsync();
            var suggestions = await _sut.GetSuggestionsAsync(new GetSuggestionsRequest()
            {
                IsAutoSuggestion = true,
            });

            suggestions.Should().BeEmpty();
        }

        [Fact]
        public async Task ShowReferenceLoggerAsync()
        {
            _referenceLogger.DidShowReferenceLogger = false;

            await _sut.ShowReferenceLoggerAsync();

            _referenceLogger.DidShowReferenceLogger.Should().BeTrue();
        }

        [Fact]
        public async Task LogReferenceAsync()
        {
            var request = new LogReferenceRequest();

            await _sut.LogReferenceAsync(request);

            _referenceLogger.LoggedReferences.Should().Contain(request);
        }
    }
}
