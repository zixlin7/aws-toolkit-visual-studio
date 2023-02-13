using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Beanstalk;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Publishing.Common;
using Amazon.AWSToolkit.Tests.Publishing.Fixtures;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class BeanstalkEnvironmentViewerCommandFactoryTests
    {

        private const string SampleEnvironmentName = "sampleEnvironment";
        private readonly PublishContextFixture _contextFixture = new PublishContextFixture();
        private readonly TestPublishToAwsDocumentViewModel _viewModel;
        private readonly Mock<IBeanstalkViewer> _beanstalkViewer = new Mock<IBeanstalkViewer>();
        private readonly ICommand _viewerCommand;
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitShellProvider;

        public BeanstalkEnvironmentViewerCommandFactoryTests()
        {
            _viewModel =
                new TestPublishToAwsDocumentViewModel(
                    new PublishApplicationContext(_contextFixture.PublishContext))
                {
                    PublishDestination = new PublishRecommendation(new RecommendationSummary()
                    {
                        DeploymentType = DeploymentTypes.BeanstalkEnvironment,
                    })
                };
            _viewModel.PublishProjectViewModel.PublishedArtifactId = SampleEnvironmentName;
            _viewerCommand = BeanstalkEnvironmentViewerCommandFactory.Create(_viewModel);
            SetupToolkitHost();
        }

        private void SetupToolkitHost()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IBeanstalkViewer)))
                .Returns(_beanstalkViewer.Object);
        }

        [Fact]
        public void CanExecute_NoPublishDestination()
        {
            _viewModel.PublishDestination = null;

            Assert.False(_viewerCommand.CanExecute(null));
        }

        [Fact]
        public void CanExecute_CloudFormationStack()
        {
            _viewModel.PublishDestination = new PublishRecommendation(new RecommendationSummary()
            {
                DeploymentType = DeploymentTypes.CloudFormationStack,
            });

            Assert.False(_viewerCommand.CanExecute(null));
        }

        [Fact]
        public void CanExecute()
        {
            Assert.True(_viewerCommand.CanExecute(null));
        }

        [Fact]
        public void ViewEnvironment()
        {
            _viewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IBeanstalkViewer)), Times.Once);
            _beanstalkViewer.Verify(view => view.ViewEnvironment(SampleEnvironmentName, It.IsAny<AwsConnectionSettings>()), Times.Once);
        }

        [Fact]
        public void ViewEnvironment_Throws()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IBeanstalkViewer)))
                .Throws<Exception>();
            _viewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IBeanstalkViewer)), Times.Once);
            _beanstalkViewer.Verify(view => view.ViewEnvironment(SampleEnvironmentName, It.IsAny<AwsConnectionSettings>()), Times.Never);
            ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(str => str.Contains("Error viewing")), true), Times.Once);
        }
    }
}
