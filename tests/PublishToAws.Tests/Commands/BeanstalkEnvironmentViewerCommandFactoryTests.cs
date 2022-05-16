using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk.Viewers;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Regions;
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
        private readonly Mock<IBeanstalkEnvironmentViewer> _environmentViewer = new Mock<IBeanstalkEnvironmentViewer>();
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
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IBeanstalkEnvironmentViewer)))
                .Returns(_environmentViewer.Object);
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

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IBeanstalkEnvironmentViewer)), Times.Once);
            _environmentViewer.Verify(view => view.View(SampleEnvironmentName, It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()), Times.Once);
        }

        [Fact]
        public void ViewEnvironment_Throws()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IBeanstalkEnvironmentViewer)))
                .Throws<Exception>();
            _viewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IBeanstalkEnvironmentViewer)), Times.Once);
            _environmentViewer.Verify(view => view.View(SampleEnvironmentName, It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()), Times.Never);
            ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(str => str.Contains("Error viewing")), true), Times.Once);
        }
    }
}
