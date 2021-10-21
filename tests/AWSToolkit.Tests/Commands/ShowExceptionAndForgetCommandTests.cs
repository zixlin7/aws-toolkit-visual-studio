using System.IO;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.Commands
{
    public class ShowExceptionAndForgetCommandTests
    {
        private readonly SpyErrorShowingToolkitShellProvider _spyToolkitHost = new SpyErrorShowingToolkitShellProvider();

        [Fact]
        public void ShouldForwardCanExecuteToCommand()
        {
            var parameter = "string";
            var command = new RelayCommand((obj) => parameter.Equals(obj), null);

            // act
            var decoratedCommand = DecorateCommand(command);
            var canExecute = decoratedCommand.CanExecute(parameter);

            // assert.
            Assert.True(canExecute);
        }

        private ICommand DecorateCommand(ICommand command) => new ShowExceptionAndForgetCommand(command, _spyToolkitHost);

        [Fact]
        public void ShouldForwardParameterToCommand()
        {
            // arrange.
            var parameter = "string";

            bool result = false;
            var command = new RelayCommand((obj) => result = parameter.Equals(obj));

            // act.
            var decoratedCommand = DecorateCommand(command);
            decoratedCommand.Execute(parameter);

            // assert.
            Assert.True(result);
        }

        [Fact]
        public void ShouldShowErrorToUser()
        {
            // arrange.
            var message = "File could not be opened";

            var command = new RelayCommand((obj) => throw new IOException(message));

            // act.
            var decoratedCommand = DecorateCommand(command);
            decoratedCommand.Execute(null);

            // assert.
            Assert.Equal(message, _spyToolkitHost.Message);
        }

        [Fact]
        public void ShouldShowErrorWithInnerExceptionToUser()
        {
            // arrange.
            var commandExceptionMessage = "Error trying to execute.";
            var innerExceptionMessage = "File is not found.";

            var command = new RelayCommand((obj) =>
                throw new CommandException(commandExceptionMessage, new IOException(innerExceptionMessage)));

            // act.
            var decoratedCommand = DecorateCommand(command);
            decoratedCommand.Execute(null);

            // assert.
            Assert.Equal(commandExceptionMessage, _spyToolkitHost.Title);
            Assert.Equal(innerExceptionMessage, _spyToolkitHost.Message);
        }

        public class SpyErrorShowingToolkitShellProvider : NoOpToolkitShellProvider
        {
            public string Title { get; private set; }
            public string Message { get; private set; }

            public override void ShowError(string message)
            {
                Message = message;
            }

            public override void ShowError(string title, string message)
            {
                Title = title;
                Message = message;
            }
        }
    }
}
