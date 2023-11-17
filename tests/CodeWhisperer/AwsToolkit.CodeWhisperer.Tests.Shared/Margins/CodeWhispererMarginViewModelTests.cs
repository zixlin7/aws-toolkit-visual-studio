using System;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Margins;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Documents;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions;
using Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Microsoft.VisualStudio.Sdk.TestFramework;
using Microsoft.VisualStudio.Shell;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Margins
{
    [Collection(VsMockCollection.CollectionName)]
    public class CodeWhispererMarginViewModelTests : IDisposable
    {
        private readonly FakeCodeWhispererTextView _textView = new FakeCodeWhispererTextView();
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly FakeSuggestionUiManager _suggestionUiManager = new FakeSuggestionUiManager();
        private readonly FakeVsCommandRepository _commandRepository = new FakeVsCommandRepository();

        private readonly ToolkitContextFixture _toolkitContext = new ToolkitContextFixture();

        private readonly CodeWhispererMarginViewModel _sut;

        public CodeWhispererMarginViewModelTests(GlobalServiceProvider globalServiceProvider)
        {
            globalServiceProvider.Reset();

            var taskFactoryProvider = new ToolkitJoinableTaskFactoryProvider(ThreadHelper.JoinableTaskContext);

            _sut = new CodeWhispererMarginViewModel(
                _textView,
                _manager,
                _suggestionUiManager,
                _commandRepository,
                _toolkitContext.ToolkitContextProvider,
                taskFactoryProvider);
        }

        [Fact]
        public void UpdateKeyBindings()
        {
            _commandRepository.CommandBinding = "Control C";

            _sut.UpdateKeyBindings();

            _sut.GenerateSuggestionsKeyBinding.Should().Be(_commandRepository.CommandBinding);
        }

        public static TheoryData<bool, LspClientStatus, ConnectionStatus, MarginStatus> GetMarginStatusInputs()
        {
            return new TheoryData<bool, LspClientStatus, ConnectionStatus, MarginStatus>()
            {
                // NotRunning and SettingUp: unconditionally Disconnected
                { false, LspClientStatus.NotRunning, ConnectionStatus.Connected, MarginStatus.Disconnected },
                { false, LspClientStatus.SettingUp, ConnectionStatus.Connected, MarginStatus.Disconnected },
                // Error: unconditionally Error
                { false, LspClientStatus.Error, ConnectionStatus.Connected, MarginStatus.Error },
                // Running: depends on other statuses
                { true, LspClientStatus.Running, ConnectionStatus.Disconnected, MarginStatus.Disconnected },
                { false, LspClientStatus.Running, ConnectionStatus.Disconnected, MarginStatus.Disconnected },
                { true, LspClientStatus.Running, ConnectionStatus.Expired, MarginStatus.Disconnected },
                { false, LspClientStatus.Running, ConnectionStatus.Expired, MarginStatus.Disconnected },
                { true, LspClientStatus.Running, ConnectionStatus.Connected, MarginStatus.ConnectedPaused },
                { false, LspClientStatus.Running, ConnectionStatus.Connected, MarginStatus.Connected },
            };
        }

        [Theory]
        [MemberData(nameof(GetMarginStatusInputs))]
        public void MarginStatusUpdates(bool isAutoSuggestionPaused, LspClientStatus clientStatus, ConnectionStatus connectionStatus, MarginStatus expectedMarginStatus)
        {
            _manager.AutomaticSuggestionsEnabled = !isAutoSuggestionPaused;
            _manager.ConnectionStatus = connectionStatus;
            _manager.ClientStatus = clientStatus;

            _manager.RaisePauseAutoSuggestChanged();
            _manager.RaiseStatusChanged();
            _manager.RaiseClientStatusChanged();

            _sut.MarginStatus.Should().Be(expectedMarginStatus);
        }

        public void Dispose()
        {
            _sut?.Dispose();
        }
    }
}
