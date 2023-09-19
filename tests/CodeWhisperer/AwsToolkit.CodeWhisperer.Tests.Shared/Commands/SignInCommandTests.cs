using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
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
        [InlineData(ConnectionStatus.Disconnected, true)]
        [InlineData(ConnectionStatus.Connected, false)]
        [InlineData(ConnectionStatus.Expired, false)]
        public void CanExecute(ConnectionStatus status, bool expectedCanExecute)
        {
            _manager.ConnectionStatus = status;
            _sut.CanExecute().Should().Be(expectedCanExecute);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            await _sut.ExecuteAsync();
            _manager.IsSignedIn.Should().BeTrue();
        }
    }
}
