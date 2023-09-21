using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Credentials;
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
        [InlineData(ConnectionStatus.Disconnected, false)]
        [InlineData(ConnectionStatus.Connected, true)]
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
            _manager.IsSignedIn.Should().BeFalse();
        }
    }
}
