using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Lambda.Command;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using AWSToolkit.Tests.CloudWatch;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class ViewLogStreamsCommandTests
    {
        private readonly ViewLogStreamsCommand _command;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly string _functionName = "sample-function";
        private readonly string _logGroup;
        private readonly Mock<ILogStreamsViewer> _logStreamsViewer = new Mock<ILogStreamsViewer>();

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public ViewLogStreamsCommandTests()
        {
            Setup();
            _logGroup =  $"/aws/lambda/{_functionName}";
            var awsConnectionSettings = new AwsConnectionSettings(null, null);
            _command = new ViewLogStreamsCommand(_functionName, _contextFixture.ToolkitContext, awsConnectionSettings);
        }

        [Fact]
        public void Execute_WhenInvalidViewer()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)))
                .Returns(null);

            var result = _command.Execute();

            ToolkitHost.Verify(
                mock => mock.OutputToHostConsole(
                    It.Is<string>(msg => msg.Contains("Unable to view CloudWatch log streams")), true),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogGroup, MetricSources.CloudWatchLogsMetricSource.LambdaNode);
        }

        [Fact]
        public void Execute()
        {
            var result = _command.Execute();

            _logStreamsViewer.Verify(
                mock => mock.View(_logGroup, It.IsAny<AwsConnectionSettings>()), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded,
                CloudWatchResourceType.LogGroup, MetricSources.CloudWatchLogsMetricSource.LambdaNode);
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogStreamsViewer)))
                .Returns(_logStreamsViewer.Object);
        }
    }
}
