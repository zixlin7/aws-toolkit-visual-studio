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
    public class CancelSecurityScanCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly CancelSecurityScanCommand _sut;

        public CancelSecurityScanCommandTests()
        {
            _sut = new CancelSecurityScanCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Theory]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, SecurityScanState.NotRunning, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, SecurityScanState.Running, true)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Running, SecurityScanState.Cancelling, false)]
        [InlineData(ConnectionStatus.Disconnected, LspClientStatus.Running, SecurityScanState.Running, false)]
        [InlineData(ConnectionStatus.Expired, LspClientStatus.Running, SecurityScanState.Running, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.SettingUp, SecurityScanState.Running, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.NotRunning, SecurityScanState.Running, false)]
        [InlineData(ConnectionStatus.Connected, LspClientStatus.Error, SecurityScanState.Running, false)]
        public void CanExecute(ConnectionStatus connectionStatus, LspClientStatus lspClientStatus,
            SecurityScanState securityScanState, bool expectedCanExecute)
        {
            _manager.ConnectionStatus = connectionStatus;
            _manager.ClientStatus = lspClientStatus;
            _manager.SecurityScanState = securityScanState;

            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            _manager.ConnectionStatus = ConnectionStatus.Connected;
            _manager.ClientStatus = LspClientStatus.Running;
            _manager.SecurityScanState = SecurityScanState.Running;

            await _sut.ExecuteAsync();
            _manager.IsScanning.Should().BeFalse();
        }
    }
}
