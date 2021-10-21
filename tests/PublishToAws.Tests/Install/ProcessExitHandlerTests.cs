using System;

using Amazon.AWSToolkit.Publish.Install;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Install
{
    public class ProcessExitHandlerTests
    {
        private ProcessExitHandler _processExitHandler;
        private const string SampleNet5ErrorMessage = "error: Unrecognized command or argument 'verify'.Specify --help";
        private readonly ProcessData _sampleProcessData = new ProcessData("", "");

        [Theory]
        [InlineData("", SampleNet5ErrorMessage)]
        [InlineData(SampleNet5ErrorMessage, "")]
        public void ExecuteThrows_WhenNet5NotPresent(string output, string error)
        {
            var data = new ProcessData(output, error);
            CreateExitHandler(data, 0);

            var exception = Assert.Throws<InvalidOperationException>(() => _processExitHandler.Execute());

            Assert.Contains("You might need to install .NET 5", exception.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void ExecuteThrows_WhenNonZeroExitCode(int exitCode)
        {
            _processExitHandler = new ProcessExitHandler(_sampleProcessData, exitCode);

            var exception = Assert.Throws<InvalidOperationException>(() => _processExitHandler.Execute());

            Assert.Contains("Restart Visual Studio to try again.", exception.Message);
        }

        [Fact]
        public void Execute()
        {
            _processExitHandler = new ProcessExitHandler(_sampleProcessData, 0);

            var exception = Record.Exception(() => _processExitHandler.Execute());

            Assert.Null(exception);
        }

        private void CreateExitHandler(ProcessData data, int exitCode)
        {
            _processExitHandler = new ProcessExitHandler(data, exitCode);
        }
    }
}
