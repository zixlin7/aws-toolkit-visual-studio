using System;
using System.Windows.Input;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Ecr;
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
    public class EcrRepoViewerCommandFactoryTests
    {

        private const string SampleRepoName = "samplerepo";
        private readonly PublishContextFixture _contextFixture = new PublishContextFixture();
        private readonly TestPublishToAwsDocumentViewModel _viewModel;
        private readonly Mock<IEcrViewer> _repoViewer = new Mock<IEcrViewer>();
        private readonly ICommand _viewerCommand;
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitShellProvider;

        public EcrRepoViewerCommandFactoryTests()
        {
            _viewModel =
                new TestPublishToAwsDocumentViewModel(
                    new PublishApplicationContext(_contextFixture.PublishContext))
                {
                    PublishedArtifactId = SampleRepoName,
                    PublishDestination = new PublishRecommendation(new RecommendationSummary()
                    {
                        DeploymentType = DeploymentTypes.ElasticContainerRegistryImage,
                    })
                };
            _viewerCommand = EcrRepoViewerCommandFactory.Create(_viewModel);
            SetupToolkitHost();
        }

        private void SetupToolkitHost()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IEcrViewer)))
                .Returns(_repoViewer.Object);
        }

        [Fact]
        public void CanExecute_NoPublishDestination()
        {
            _viewModel.PublishDestination = null;

            Assert.False(_viewerCommand.CanExecute(null));
        }

        [Theory]
        [InlineData(DeploymentTypes.CloudFormationStack)]
        [InlineData(DeploymentTypes.BeanstalkEnvironment)]
        public void CanExecute_NonRepoType(DeploymentTypes deploymentType)
        {
            _viewModel.PublishDestination = new PublishRecommendation(new RecommendationSummary()
            {
                DeploymentType = deploymentType,
            });

            Assert.False(_viewerCommand.CanExecute(null));
        }

        [Fact]
        public void CanExecute()
        {
            Assert.True(_viewerCommand.CanExecute(null));
        }

        [Fact]
        public void ViewRepository()
        {
            _viewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IEcrViewer)), Times.Once);
            _repoViewer.Verify(view => view.ViewRepository(SampleRepoName, It.IsAny<AwsConnectionSettings>()), Times.Once);
        }

        [Fact]
        public void ViewRepository_NoRepoName()
        {
            _viewModel.PublishedArtifactId = "";
            _viewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IEcrViewer)), Times.Never);
            _repoViewer.Verify(
                view => view.ViewRepository(SampleRepoName, It.IsAny<AwsConnectionSettings>()),
                Times.Never);
            ToolkitHost.Verify(
                host => host.OutputToHostConsole(It.Is<string>(str => str.StartsWith("Unable to view ECR Repository")), true),
                Times.Once);
            ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(str => str.Contains("No ECR Repository name")), true), Times.Once);
        }

        [Fact]
        public void ViewRepository_Throws()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(IEcrViewer)))
                .Throws<Exception>();
            _viewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(IEcrViewer)), Times.Once);
            _repoViewer.Verify(
                view => view.ViewRepository(SampleRepoName, It.IsAny<AwsConnectionSettings>()),
                Times.Never);
            ToolkitHost.Verify(
                host => host.OutputToHostConsole(It.Is<string>(str => str.StartsWith("Unable to view ECR Repository")), true),
                Times.Once);
        }
    }
}
