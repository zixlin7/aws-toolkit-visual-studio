using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Clients;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class SignInCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly SignInCommand _sut;

        public SignInCommandTests()
        {
            _sut = new SignInCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Theory]
        [InlineData(LspClientStatus.Running, ConnectionStatus.Disconnected, true)]
        [InlineData(LspClientStatus.Error, ConnectionStatus.Disconnected, false)]
        [InlineData(LspClientStatus.SettingUp, ConnectionStatus.Disconnected, false)]
        [InlineData(LspClientStatus.NotRunning, ConnectionStatus.Disconnected, false)]
        [InlineData(LspClientStatus.Running, ConnectionStatus.Connected, false)]
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
            _manager.ConnectionStatus = ConnectionStatus.Disconnected;
            _manager.ClientStatus = LspClientStatus.Running;

            await _sut.ExecuteAsync();
            _manager.IsSignedIn.Should().BeTrue();
        }
    }
}
