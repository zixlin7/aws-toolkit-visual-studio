using System;

using Amazon.AWSToolkit.CloudWatch.Logs.Commands;
using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.ToolWindow;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using AWSToolkit.Tests.CloudWatch.Logs.Util;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Logs.Commands
{
    public class ViewLogGroupsCommandTests
    {
        private readonly ViewLogGroupsCommand _command;

        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<IToolWindowFactory> _toolWindowFactory = new Mock<IToolWindowFactory>();
        private readonly Mock<IRepositoryFactory> _repoFactory = new Mock<IRepositoryFactory>();
        private readonly Mock<ICloudWatchLogsRepository> _cwlRepository = new Mock<ICloudWatchLogsRepository>();
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public ViewLogGroupsCommandTests()
        {
            var awsConnectionSettings = new AwsConnectionSettings(null, null);

            Setup();
            _command = new ViewLogGroupsCommand(AwsExplorerMetricSource.CloudWatchLogsNode, _contextFixture.ToolkitContext, awsConnectionSettings);
        }

        [Fact]
        public void Execute_WhenToolWindowThrows()
        {
            ToolkitHost.Setup(mock => mock.GetToolWindowFactory())
                .Throws(new InvalidOperationException());
            var result = _command.Execute();
            Assert.False(result.Success);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Failed, CloudWatchResourceType.LogGroupList);
        }

        [StaFact]
        public void Execute()
        {
            var result = _command.Execute();
            Assert.True(result.Success);

            ToolkitHost.Verify(host => host.GetToolWindowFactory(), Times.Once);
            _toolWindowFactory.Verify(mock => mock.ShowLogGroupsToolWindow(It.IsAny<BaseAWSControl>(), It.IsAny<Func<BaseAWSControl, bool>>()), Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsOpen(Result.Succeeded, CloudWatchResourceType.LogGroupList);
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.GetToolWindowFactory())
                .Returns(_toolWindowFactory.Object);
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(_repoFactory.Object);
            _repoFactory
                .Setup(mock =>
                    mock.CreateCloudWatchLogsRepository(It.IsAny<AwsConnectionSettings>()))
                .Returns(_cwlRepository.Object);
        }
    }
}
