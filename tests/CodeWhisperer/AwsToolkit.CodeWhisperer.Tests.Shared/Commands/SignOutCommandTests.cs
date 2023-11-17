using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class SignOutCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly SignOutCommand _sut;

        public SignOutCommandTests()
        {
            _sut = new SignOutCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Theory]
        [InlineData(LspClientStatus.Running, ConnectionStatus.Disconnected, false)]
        [InlineData(LspClientStatus.Running, ConnectionStatus.Connected, true)]
        [InlineData(LspClientStatus.Error, ConnectionStatus.Connected, false)]
        [InlineData(LspClientStatus.NotRunning, ConnectionStatus.Connected, false)]
        [InlineData(LspClientStatus.SettingUp, ConnectionStatus.Connected, false)]
        [InlineData(LspClientStatus.Running, ConnectionStatus.Expired, false)]
        public void CanExecute(LspClientStatus clientStatus, ConnectionStatus connectionStatus, bool expectedCanExecute)
        {
            _manager.ConnectionStatus = connectionStatus;
            _manager.ClientStatus = clientStatus;
            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            _manager.ConnectionStatus = ConnectionStatus.Connected;
            _manager.ClientStatus = LspClientStatus.Running;

            await _sut.ExecuteAsync();
            _manager.IsSignedIn.Should().BeFalse();
        }
    }
}
