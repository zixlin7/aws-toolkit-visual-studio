using System;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Credentials.Core;
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
    public class StackViewerCommandTests
    {

        private const string SampleStackName = "sampleStack";
        private readonly PublishContextFixture _contextFixture = new PublishContextFixture();
        private readonly TestPublishToAwsDocumentViewModel _viewModel;
        private readonly Mock<ICloudFormationViewer> _cloudFormationViewer = new Mock<ICloudFormationViewer>();
        private readonly ICommand _stackViewerCommand;
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitShellProvider;

        public StackViewerCommandTests()
        {
            _viewModel =
                new TestPublishToAwsDocumentViewModel(
                    new PublishApplicationContext(_contextFixture.PublishContext))
                {
                    PublishedArtifactId = SampleStackName,
                    PublishDestination = new PublishRecommendation(new RecommendationSummary()
                    {
                        DeploymentType = DeploymentTypes.CloudFormationStack,
                    })
                };
            _stackViewerCommand = StackViewerCommandFactory.Create(_viewModel);
            SetupToolkitHost();
        }

        private void SetupToolkitHost()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)))
                .Returns(_cloudFormationViewer.Object);
        }

        [Fact]
        public void CanExecute_NoPublishDestination()
        {
            _viewModel.PublishDestination = null;

            Assert.False(_stackViewerCommand.CanExecute(null));
        }

        [Fact]
        public void CanExecute_BeanstalkEnvironment()
        {
            _viewModel.PublishDestination = new PublishRecommendation(new RecommendationSummary()
            {
                DeploymentType = DeploymentTypes.BeanstalkEnvironment,
            });

            Assert.False(_stackViewerCommand.CanExecute(null));
        }

        [Fact]
        public void ViewStack()
        {
            _stackViewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)), Times.Once);
            _cloudFormationViewer.Verify(view => view.View(SampleStackName, It.IsAny<AwsConnectionSettings>()), Times.Once);
        }

        [Fact]
        public void ViewStack_Throws()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)))
                .Throws<Exception>();
            _stackViewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)), Times.Once);
            _cloudFormationViewer.Verify(view => view.View(SampleStackName, It.IsAny<AwsConnectionSettings>()), Times.Never);
            ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(str => str.Contains("Error viewing")), true), Times.Once);
        }
    }
}
