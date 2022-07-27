using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.CloudWatch.Logs.Commands;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using AWSToolkit.Tests.CloudWatch.Logs.Util;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Logs.Commands
{
    public class ViewLogStreamsCommandTests
    {
        private readonly ICommand _command;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ILogStreamsViewer> _logStreamsViewer = new Mock<ILogStreamsViewer>();
        private readonly string _sampleLogGroup = "sample-group";

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public ViewLogStreamsCommandTests()
        {
            Setup();
            var awsConnectionSettings = new AwsConnectionSettings(null, null);
            _command = ViewLogStreamsCommand.Create(_contextFixture.ToolkitContext, awsConnectionSettings);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(1)]
        public void Execute_InvalidLogGroup(object parameter)
        {
            _command.Execute(parameter);

            ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Parameter is not of expected type"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogGroup, CloudWatchLogsMetricSource.LogGroupsView);
        }

        [Fact]
        public void Execute_WhenInvalidViewer()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)))
                .Returns(null);

            _command.Execute(_sampleLogGroup);

            ToolkitHost.Verify(mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Unable to load CloudWatch"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogGroup, CloudWatchLogsMetricSource.LogGroupsView);
        }

        [StaFact]
        public void Execute()
        {
            _command.Execute(_sampleLogGroup);

            _logStreamsViewer.Verify(
                mock => mock.View(_sampleLogGroup, It.IsAny<AwsConnectionSettings>()), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded,
                CloudWatchResourceType.LogGroup, CloudWatchLogsMetricSource.LogGroupsView);
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)))
                .Returns(_logStreamsViewer.Object);
        }
    }
}
