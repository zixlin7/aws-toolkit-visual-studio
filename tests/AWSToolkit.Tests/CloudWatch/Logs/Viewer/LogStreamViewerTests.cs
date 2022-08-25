using System;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Viewers;
using Amazon.AWSToolkit.CloudWatch.Logs.Views;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Logs.Viewer
{
    public class LogStreamViewerTests
    {
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<IRepositoryFactory> _repoFactory = new Mock<IRepositoryFactory>();
        private readonly Mock<ICloudWatchLogsRepository> _cwlRepository = new Mock<ICloudWatchLogsRepository>();
        private readonly LogStreamsViewer _streamViewer;
        private readonly AwsConnectionSettings _connectionSettings = new AwsConnectionSettings(null, null);
        private readonly string _logGroup = "sample-logGroup";

        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public LogStreamViewerTests()
        {
            Setup();
            _streamViewer = new LogStreamsViewer(_contextFixture.ToolkitContext);
        }

        [Fact]
        public void View_WhenInvalidRepository()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(null);

            Assert.Throws<Exception>(() => _streamViewer.View(_logGroup, _connectionSettings));
        }

        [StaFact]
        public void View()
        {
            _streamViewer.View(_logGroup, _connectionSettings);

            ToolkitHost.Verify(
                mock => mock.OpenInEditor(It.Is<IAWSToolkitControl>(control =>
                    control.GetType() == typeof(LogStreamsViewerControl))), Times.Once);
        }

        [Fact]
        public void GetViewer_WhenInvalidRepository()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(null);

            Assert.Throws<Exception>(() => _streamViewer.GetViewer(_logGroup, _connectionSettings));
        }

        [StaFact]
        public void GetViewer()
        {
            var view = _streamViewer.GetViewer(_logGroup, _connectionSettings);
            Assert.IsType<LogStreamsViewerControl>(view);
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
