using System;

using Amazon.AWSToolkit.CloudWatch;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Views;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public class LogEventsViewerTests
    {
        private readonly ILogEventsViewer _eventsViewer;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<IRepositoryFactory> _repoFactory = new Mock<IRepositoryFactory>();
        private readonly Mock<ICloudWatchLogsRepository> _cwlRepository = new Mock<ICloudWatchLogsRepository>();
        private readonly AwsConnectionSettings _connectionSettings = new AwsConnectionSettings(null, null);
        private readonly string _logGroup = "sample-log-group";
        private readonly string _logStream = "sample-log-stream";

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public LogEventsViewerTests()
        {
            Setup();
            _eventsViewer = new LogEventsViewer(_contextFixture.ToolkitContext);
        }

        [Fact]
        public void View_WhenInvalidRepository()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(null);

            Assert.Throws<Exception>(() => _eventsViewer.View(_logGroup, _logStream, _connectionSettings));
        }

        [StaFact]
        public void Execute()
        {
            _eventsViewer.View(_logGroup, _logStream, _connectionSettings);

            ToolkitHost.Verify(
                mock => mock.OpenInEditor(It.Is<IAWSToolkitControl>(control =>
                    control.GetType() == typeof(LogEventsViewerControl))), Times.Once);
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
