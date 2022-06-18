using System.Collections.Generic;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Commands
{
    public class ViewLogEventsCommandTests
    {
        private readonly ICommand _command;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<IRepositoryFactory> _repoFactory = new Mock<IRepositoryFactory>();
        private readonly Mock<ICloudWatchLogsRepository> _cwlRepository = new Mock<ICloudWatchLogsRepository>();
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
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogStream);
        }

        public static IEnumerable<object[]> InvalidParameters = new List<object[]>
        {
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
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogStream);
        }

        [Fact]
        public void Execute_WhenInvalidRepository()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
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

            ToolkitHost.Verify(
                mock => mock.OpenInEditor(It.Is<IAWSToolkitControl>(control =>
                    control.GetType() == typeof(LogEventsViewerControl))), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded, CloudWatchResourceType.LogStream);
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
