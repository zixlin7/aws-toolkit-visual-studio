using System;

using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.ToolWindow;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public class ViewLogGroupsCommandTests
    {
        private readonly ViewLogGroupsCommand _command;
        private readonly LogGroupsRootViewModel _rootViewModel;

        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<IToolWindowFactory> _toolWindowFactory = new Mock<IToolWindowFactory>();
        private readonly Mock<IRepositoryFactory> _repoFactory = new Mock<IRepositoryFactory>();
        private readonly Mock<ICloudWatchLogsRepository> _cwlRepository = new Mock<ICloudWatchLogsRepository>();
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitHost;

        public ViewLogGroupsCommandTests()
        {
            Setup();
            _command = new ViewLogGroupsCommand(_contextFixture.ToolkitContext);
            var cloudWatchRootModel = new TestCloudWatchRootViewModel(null, null, null, null, _contextFixture.ToolkitContext);
            _rootViewModel =
                new LogGroupsRootViewModel(null, cloudWatchRootModel, _contextFixture.ToolkitContext);
        }

        [Fact]
        public void Execute_WhenModelNull()
        {
            var result = _command.Execute(null);
            Assert.False(result.Success);
        }

        [Fact]
        public void Execute_WhenToolWindowThrows()
        {
            ToolkitHost.Setup(mock => mock.GetToolWindowFactory())
                .Throws(new InvalidOperationException());
            var result = _command.Execute(_rootViewModel);
            Assert.False(result.Success);
        }

        [StaFact]
        public void Execute()
        {
            var result = _command.Execute(_rootViewModel);
            Assert.True(result.Success);

            ToolkitHost.Verify(host => host.GetToolWindowFactory(), Times.Once);
            _toolWindowFactory.Verify(mock => mock.ShowLogGroupsToolWindow(It.IsAny<BaseAWSControl>(), It.IsAny<Func<BaseAWSControl, bool>>()), Times.Once);
        }

        private void Setup()
        {
            ToolkitHost.Setup(mock => mock.GetToolWindowFactory())
                .Returns(_toolWindowFactory.Object);
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IRepositoryFactory)))
                .Returns(_repoFactory.Object);
            _repoFactory
                .Setup(mock =>
                    mock.CreateCloudWatchLogsRepository(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns(_cwlRepository.Object);
        }
    }
}
