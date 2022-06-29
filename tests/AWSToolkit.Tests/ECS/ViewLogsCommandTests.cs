using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using AWSToolkit.Tests.CloudWatch;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.ECS
{
    public class ViewLogsCommandTests
    {
        private readonly ICommand _command;
        private readonly AwsConnectionSettings _connectionSettings = new AwsConnectionSettings(null, null);
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<ILogEventsViewer> _logEventsViewer = new Mock<ILogEventsViewer>();
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public ViewLogsCommandTests()
        {
            Setup();
            _command = ViewLogsCommand.Create(_contextFixture.ToolkitContext, _connectionSettings);
        }

        public static IEnumerable<object[]> InvalidParameters = new List<object[]>
        {
            new object[] { new object[] { null } },
            new object[] { new object[] { 1 } },
            new object[] { new object[] { "hello" } },
            new object[] { new object[] { new List<string> { "hello", "bye" } } },
            new object[] { new object[] { new Dictionary<string, string>() } }
        };

        [Theory]
        [MemberData(nameof(InvalidParameters))]
        public void Execute_InvalidParameters(object parameter)
        {
            _command.Execute(parameter);

            ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Parameter is not of expected type"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogStream, MetricSources.CloudWatchLogsMetricSource.ClusterTaskView);
        }

        [Fact]
        public void Execute_WhenInvalidViewer()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogEventsViewer)))
                .Returns(null);
            var containerToLogs = CreateSampleContainerToLogs(1);

            _command.Execute(containerToLogs);

            ToolkitHost.Verify(mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Unable to load CloudWatch"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogStream, MetricSources.CloudWatchLogsMetricSource.ClusterTaskView);
        }

        [Fact]
        public void Execute_NoContainers()
        {
            var containerToLogs = new Dictionary<string, LogProperties>();

            _command.Execute(containerToLogs);

            ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("No containers to view logs from"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogStream, MetricSources.CloudWatchLogsMetricSource.ClusterTaskView);
        }

        [StaFact]
        public void Execute_ContainerSelectionCancelled()
        {
            SetupHostModalDialog(false);
            var containerToLogs = CreateSampleContainerToLogs(2);

            _command.Execute(containerToLogs);

            _logEventsViewer.Verify(
                mock => mock.View(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AwsConnectionSettings>()),
                Times.Never);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed,
                CloudWatchResourceType.LogStream, MetricSources.CloudWatchLogsMetricSource.ClusterTaskView);
        }

        [Fact]
        public void Execute_WithSingleContainer()
        {
            SetupHostModalDialog(true);
            var containerToLogs = CreateSampleContainerToLogs(1);

            _command.Execute(containerToLogs);

            var expectedProperties = containerToLogs.Values.First();
            _logEventsViewer.Verify(
                mock => mock.View(expectedProperties.LogGroup, expectedProperties.LogStream,
                    It.IsAny<AwsConnectionSettings>()), Times.Once);
            ToolkitHost.Verify(x =>
                x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>()), Times.Never);

            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded,
                CloudWatchResourceType.LogStream, MetricSources.CloudWatchLogsMetricSource.ClusterTaskView);
        }

        [StaFact]
        public void Execute_WitMultipleContainer()
        {
            SetupHostModalDialog(true);
            var containerToLogs = CreateSampleContainerToLogs(2);

            _command.Execute(containerToLogs);

            var expectedProperties = containerToLogs.Values.First();
            _logEventsViewer.Verify(
                mock => mock.View(expectedProperties.LogGroup, expectedProperties.LogStream,
                    It.IsAny<AwsConnectionSettings>()), Times.Once);

            ToolkitHost.Verify(x =>
                x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>()), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded,
                CloudWatchResourceType.LogStream, MetricSources.CloudWatchLogsMetricSource.ClusterTaskView);
        }

        private static Dictionary<string, LogProperties> CreateSampleContainerToLogs(int count)
        {
            var dict = new Dictionary<string, LogProperties>();
            Enumerable.Range(1, count).ToList().ForEach(i =>
            {
                var guid = Guid.NewGuid().ToString();
                var properties = new LogProperties() { LogGroup = $"lg-{guid}", LogStream = $"ls-{guid}" };
                dict.Add(guid, properties);
            });
            return dict;
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ILogEventsViewer)))
                .Returns(_logEventsViewer.Object);
            SetupHostModalDialog(true);
        }

        private void SetupHostModalDialog(bool returnValue)
        {
            ToolkitHost.Setup(x =>
                    x.ShowInModalDialogWindow(It.IsAny<IAWSToolkitControl>(), It.IsAny<MessageBoxButton>()))
                .Returns(returnValue);
        }
    }
}
