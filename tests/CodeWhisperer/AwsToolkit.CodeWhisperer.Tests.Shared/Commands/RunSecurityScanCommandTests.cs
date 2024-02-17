using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.SecurityScans.Models;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Documents;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class RunSecurityScanCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly FakeCodeWhispererTextView _textView = new FakeCodeWhispererTextView();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly RunSecurityScanCommand _sut;

        public RunSecurityScanCommandTests()
        {
            _sut = new RunSecurityScanCommand(_manager, _textView, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Theory]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, "test", SecurityScanState.NotRunning, true)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, "test", SecurityScanState.Running, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, "test", SecurityScanState.Cancelling, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, "", SecurityScanState.NotRunning, false)]
        [InlineData(ConnectionStatus.Disconnected, LspClientStatus.Running, "test", SecurityScanState.NotRunning, false)]
        [InlineData(ConnectionStatus.Expired, LspClientStatus.Running, "test", SecurityScanState.NotRunning, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.SettingUp, "test", SecurityScanState.NotRunning, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.NotRunning, "test", SecurityScanState.NotRunning,  false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Error, "test", SecurityScanState.NotRunning, false)]
        public void CanExecute(ConnectionStatus connectionStatus, LspClientStatus lspClientStatus, string filePath, SecurityScanState securityScanState, bool expectedCanExecute)
        {
            _manager.ConnectionStatus = connectionStatus;
            _manager.ClientStatus = lspClientStatus;
            _manager.SecurityScanState = securityScanState;
            _textView.FilePath = filePath;

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            _manager.ConnectionStatus = ConnectionStatus.Connected;
            _manager.ClientStatus = LspClientStatus.Running;
            _manager.SecurityScanState = SecurityScanState.NotRunning;
            _textView.FilePath = "test";
            await _sut.ExecuteAsync();
            _manager.IsScanning.Should().BeTrue();
        }
    }
}
