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

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Commands
{
    public class StackViewerCommandTests
    {

        private const string SampleStackName = "sampleStack";
        private readonly PublishContextFixture _contextFixture = new PublishContextFixture();
        private readonly Mock<ICloudFormationViewer> _cloudFormationViewer = new Mock<ICloudFormationViewer>();
        private readonly ICommand _stackViewerCommand;
        private Mock<IAWSToolkitShellProvider> ToolkitHost => _contextFixture.ToolkitShellProvider;

        public StackViewerCommandTests()
        {
            var viewModel =
                new TestPublishToAwsDocumentViewModel(
                    new PublishApplicationContext(_contextFixture.PublishContext))
                { PublishedStackName = SampleStackName };
            _stackViewerCommand = StackViewerCommandFactory.Create(viewModel);
            SetupToolkitHost();
        }

        private void SetupToolkitHost()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)))
                .Returns(_cloudFormationViewer.Object);
        }

        [Fact]
        public void ViewStack()
        {
            _stackViewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)), Times.Once);
            _cloudFormationViewer.Verify(view => view.View(SampleStackName, It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()), Times.Once);
        }

        [Fact]
        public void ViewStack_Throws()
        {
            ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)))
                .Throws<Exception>();
            _stackViewerCommand.Execute(null);

            ToolkitHost.Verify(host => host.QueryAWSToolkitPluginService(typeof(ICloudFormationViewer)), Times.Once);
            _cloudFormationViewer.Verify(view => view.View(SampleStackName, It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()), Times.Never);
            ToolkitHost.Verify(host => host.OutputToHostConsole(It.Is<string>(str => str.Contains("Error viewing")), true), Times.Once);
        }
    }
}
