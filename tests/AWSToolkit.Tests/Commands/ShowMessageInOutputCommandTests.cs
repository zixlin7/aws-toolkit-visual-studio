using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.Commands
{
    public class ShowMessageInOutputCommandTests
    {
        private readonly SpyOutputToolkitShellProvider _spyShellProvider;
        private readonly ICommand _command;

        public ShowMessageInOutputCommandTests()
        {
            _spyShellProvider = new SpyOutputToolkitShellProvider();
            _command = ShowMessageInOutputCommand.Create(_spyShellProvider);
        }

        [Fact]
        public void Execute()
        {
            var message = "Filter pattern does not match the expected syntax";
            _command.Execute(message);

            Assert.Equal(message, _spyShellProvider.Message);
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(false)]
        [InlineData(2.22)]
        public void Execute_InvalidInput(object input)
        {
            _command.Execute(input);

            Assert.Null(_spyShellProvider.Message);
        }
    }

    public class SpyOutputToolkitShellProvider : NoOpToolkitShellProvider
    {
        public string Message { get; private set; }

        public override void OutputToHostConsole(string message, bool forceVisible)
        {
            Message = message;
        }
    }
}
