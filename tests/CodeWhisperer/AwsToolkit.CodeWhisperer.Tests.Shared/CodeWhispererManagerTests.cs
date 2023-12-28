using System;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AWSToolkit.Util;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;

using Xunit;
using Amazon.AwsToolkit.CodeWhisperer.Tests.SecurityScan;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests
{
    [Collection(VsMockCollection.CollectionName)]
    public class CodeWhispererManagerTests
    {
        private readonly FakeCodeWhispererClient _lspClient = new FakeCodeWhispererClient();
        private readonly FakeConnection _connection = new FakeConnection();
        private readonly FakeSuggestionProvider _suggestionProvider = new FakeSuggestionProvider();
        private readonly FakeReferenceLogger _referenceLogger = new FakeReferenceLogger();
        private readonly FakeSecurityScanProvider _securityScanProvider = new FakeSecurityScanProvider();
        private readonly CodeWhispererManager _sut;

        public CodeWhispererManagerTests(GlobalServiceProvider serviceProvider)
        {
            serviceProvider.Reset();

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);
            _sut = new CodeWhispererManager(_lspClient, _connection, _suggestionProvider, _referenceLogger, _securityScanProvider ,taskFactoryProvider);
        }

        [Theory]
        [InlineData(LspClientStatus.Error)]
        [InlineData(LspClientStatus.NotRunning)]
        [InlineData(LspClientStatus.Running)]
        [InlineData(LspClientStatus.SettingUp)]
        public void GetClientStatus(LspClientStatus expectedStatus)
        {
            _lspClient.Status = expectedStatus;

            _sut.ClientStatus.Should().Be(expectedStatus);
        }

        [Fact]
        public void ClientStatusChanged()
        {
            _lspClient.Status = LspClientStatus.Error;

            var eventArgs = Assert.Raises<LspClientStatusChangedEventArgs>(
                attach => _sut.ClientStatusChanged += attach,
                detach => _sut.ClientStatusChanged -= detach,
                () =>
                {
                    _lspClient.Status = LspClientStatus.Running;
                    _lspClient.RaiseStatusChanged();
                });

            eventArgs.Arguments.ClientStatus.Should().Be(LspClientStatus.Running);
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
            var invocationTime = DateTime.Now.AsUnixMilliseconds();
            _suggestionProvider.SuggestionSession.SessionId = "sample-sessionId";
            _suggestionProvider.SuggestionSession.Suggestions.Add(new Suggestion());
            _suggestionProvider.SuggestionSession.RequestedAtEpoch = invocationTime;

            var suggestionSession = await _sut.GetSuggestionsAsync(new GetSuggestionsRequest());

            suggestionSession.Should().BeEquivalentTo(_suggestionProvider.SuggestionSession);
            suggestionSession.SessionId.Should().BeEquivalentTo("sample-sessionId");
            suggestionSession.RequestedAtEpoch.Should().Be(invocationTime);
        }

        [Fact]
        public async Task GetSuggestionsAsync_WhenPaused()
        {
            _suggestionProvider.SuggestionSession.SessionId = "sample-sessionId";
            _suggestionProvider.SuggestionSession.Suggestions.Add(new Suggestion());
            _suggestionProvider.SuggestionSession.RequestedAtEpoch = DateTime.Now.AsUnixMilliseconds();

            await _sut.PauseAutoSuggestAsync();
            var suggestionSession = await _sut.GetSuggestionsAsync(new GetSuggestionsRequest()
            {
                IsAutoSuggestion = true,
            });

            suggestionSession.SessionId.Should().BeNullOrWhiteSpace();
            suggestionSession.Suggestions.Should().BeEmpty();
            suggestionSession.RequestedAtEpoch.Should().Be(default);
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

        [Fact]
        public async Task SendSessionCompletionResultAsync()
        {
            var sessionResult = new LogInlineCompletionSessionResultsParams(){SessionId = "sample-sessionId"};

            await _sut.SendSessionCompletionResultAsync(sessionResult);

            _lspClient.SuggestionSessionResultsPublisher.SessionResultsParam.Should().Be(sessionResult);
        }

        [Fact]
        public async Task GetScanFindingsAsync()
        {
            await _sut.GetScanFindingAsync();

            _securityScanProvider.DidRunSecurityScan.Should().Be(true);
        }
    }
}
