using System.Collections.Generic;
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
    public class ViewLogEventsCommandTests
    {
        private readonly ICommand _command;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ILogEventsViewer> _logEventsViewer = new Mock<ILogEventsViewer>();
        private readonly object[] _eventsParameters = { "sample-log-group", "sample-log-stream" };

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;


        public ViewLogEventsCommandTests()
        {
            Setup();
            var awsConnectionSettings = new AwsConnectionSettings(null, null);
            _command = ViewLogEventsCommand.Create(_contextFixture.ToolkitContext, awsConnectionSettings);
        }

        public static IEnumerable<object[]> InvalidParameterTypes = new List<object[]>
        {
            new object[] { new object[] { 1, "hello" } },
            new object[] { new object[] { false, 1 } },
            new object[] { new object[] { 2, null } },
        };


        [Theory]
        [MemberData(nameof(InvalidParameterTypes))]
        public void Execute_InvalidParameterTypes(object parameter)
        {
            _command.Execute(parameter);

            ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Parameters are not of expected type"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogStream, CloudWatchLogsMetricSource.LogGroupView);
        }

        public static IEnumerable<object[]> InvalidParameters = new List<object[]>
        {
            new object[] { "hello" },
            new object[] { false },
            new object[] { new object[] { null } },
            new object[] { new object[] { "hello" } },
            new object[] { new object[] { "great", 1, "bad" } },
        };

        [Theory]
        [MemberData(nameof(InvalidParameters))]
        public void Execute_InvalidParameters(object parameter)
        {
            _command.Execute(parameter);

            ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Expected parameters: 2"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogStream, CloudWatchLogsMetricSource.LogGroupView);
        }

        [Fact]
        public void Execute_WhenInvalidViewer()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogEventsViewer)))
                .Returns(null);

            _command.Execute(_eventsParameters);

            ToolkitHost.Verify(mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Unable to load CloudWatch"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogStream);
        }

        [StaFact]
        public void Execute()
        {
            _command.Execute(_eventsParameters);

            var logGroup = _eventsParameters[0] as string;
            var logStream = _eventsParameters[1] as string;
            _logEventsViewer.Verify(
                mock => mock.View(logGroup, logStream, It.IsAny<AwsConnectionSettings>()), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded, CloudWatchResourceType.LogStream);
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogEventsViewer)))
                .Returns(_logEventsViewer.Object);
        }
    }
}
