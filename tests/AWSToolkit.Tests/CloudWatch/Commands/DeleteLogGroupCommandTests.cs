using System.Threading;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Models;

using AWSToolkit.Tests.CloudWatch.Fixtures;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Commands
{
    public class DeleteLogGroupCommandTests
    {
        private readonly ICommand _command;
        private readonly LogGroupsViewModelFixture _groupsFixture = new LogGroupsViewModelFixture();
        private readonly LogGroup _sampleLogGroup = new LogGroup { Name = "lg", Arn = "lg-arn" };

        public DeleteLogGroupCommandTests()
        {
            var viewModel = _groupsFixture.CreateViewModel();
            viewModel.RefreshCommand = RefreshLogsCommand.Create(viewModel);

            _command = DeleteLogGroupCommand.Create(viewModel, _groupsFixture.ToolkitHost.Object);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData("hello")]
        [InlineData(1)]
        public void Execute_InvalidLogGroup(object parameter)
        {
            _command.Execute(parameter);

            _groupsFixture.ToolkitHost.Verify(
                mock => mock.ShowError(It.IsAny<string>(),
                    It.Is<string>(msg => msg.Contains("Parameter is not of expected type"))),
                Times.Once);
            _groupsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsDelete(Result.Failed, CloudWatchResourceType.LogGroup);
        }

        [Fact]
        public void Execute_WhenCancelled()
        {
            _groupsFixture.SetupToolkitHostConfirm(false);

            _command.Execute(_sampleLogGroup);

            _groupsFixture.Repository.Verify(
                mock => mock.DeleteLogGroupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _groupsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsDelete(Result.Cancelled, CloudWatchResourceType.LogGroup);
        }

        [Fact]
        public void Execute()
        {
            Setup();
            _command.Execute(_sampleLogGroup);

            _groupsFixture.Repository.Verify(
                mock => mock.DeleteLogGroupAsync(It.Is<string>(s => s.Equals(_sampleLogGroup.Name)),
                    It.IsAny<CancellationToken>()), Times.Once);
            _groupsFixture.Repository.Verify(
                mock => mock.GetLogGroupsAsync(It.IsAny<GetLogGroupsRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
            _groupsFixture.ContextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsDelete(Result.Succeeded, CloudWatchResourceType.LogGroup);
        }

        private void Setup()
        {
            _groupsFixture.SetupToolkitHostConfirm(true);
            _groupsFixture.Repository.Setup(
                mock => mock.DeleteLogGroupAsync(It.IsAny<string>(),
                    It.IsAny<CancellationToken>())).ReturnsAsync(true);
        }
    }
}
