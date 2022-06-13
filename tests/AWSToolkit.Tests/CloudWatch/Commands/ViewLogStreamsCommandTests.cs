using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Commands
{
    public class ViewLogStreamsCommandTests
    {
        private readonly ICommand _command;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly LogGroup _sampleLogGroup = new LogGroup() { Name = "sample-group", Arn = "sample-group-arn" };

        private readonly Mock<IRepositoryFactory> _repoFactory = new Mock<IRepositoryFactory>();
        private readonly Mock<ICloudWatchLogsRepository> _cwlRepository = new Mock<ICloudWatchLogsRepository>();

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public ViewLogStreamsCommandTests()
        {
            Setup();
            var awsConnectionSettings = new AwsConnectionSettings(null, null);
            _command = ViewLogStreamsCommand.Create(_contextFixture.ToolkitContext, awsConnectionSettings);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData("hello")]
        [InlineData(1)]
        public void Execute_InvalidLogGroup(object parameter)
        {
            _command.Execute(parameter);

            ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Parameter is not of expected type"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogGroup);
        }

        [Fact]
        public void Execute_WhenInvalidRepository()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(null);

            _command.Execute(_sampleLogGroup);

            ToolkitHost.Verify(mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Unable to load CloudWatch"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogGroup);
        }

        [StaFact]
        public void Execute()
        {
            _command.Execute(_sampleLogGroup);

            ToolkitHost.Verify(
                mock => mock.OpenInEditor(It.Is<IAWSToolkitControl>(control =>
                    control.GetType() == typeof(LogStreamsViewerControl))), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded, CloudWatchResourceType.LogGroup);
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(_repoFactory.Object);
            _repoFactory
                .Setup(mock =>
                    mock.CreateCloudWatchLogsRepository(It.IsAny<AwsConnectionSettings>()))
                .Returns(_cwlRepository.Object);
        }
    }
}
