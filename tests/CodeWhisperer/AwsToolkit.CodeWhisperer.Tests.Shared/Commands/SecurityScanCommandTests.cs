using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Documents;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class SecurityScanCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly FakeCodeWhispererTextView _textView = new FakeCodeWhispererTextView();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly SecurityScanCommand _sut;

        public SecurityScanCommandTests()
        {
            _sut = new SecurityScanCommand(_manager, _textView, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Theory]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, "test", true)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, "", false)]
        [InlineData(ConnectionStatus.Disconnected, LspClientStatus.Running, "test", false)]
        [InlineData(ConnectionStatus.Expired, LspClientStatus.Running, "test", false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.SettingUp, "test", false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.NotRunning, "test", false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Error, "test", false)]
        public void CanExecute(ConnectionStatus connectionStatus, LspClientStatus lspClientStatus, string filePath, bool expectedCanExecute)
        {
            _manager.ConnectionStatus = connectionStatus;
            _manager.ClientStatus = lspClientStatus;
            _textView.FilePath = filePath;

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            _manager.ConnectionStatus = ConnectionStatus.Connected;
            _manager.ClientStatus = LspClientStatus.Running;
            _textView.FilePath = "test";
            await _sut.ExecuteAsync();
            _manager.DidRunSecurityScan.Should().BeTrue();
        }
    }
}
