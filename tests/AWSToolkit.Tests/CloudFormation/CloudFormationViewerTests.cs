using System;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Shared;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudFormation
{
    public class CloudFormationViewerTests
    {
        private readonly Mock<IAWSToolkitShellProvider> _shellProvider = new Mock<IAWSToolkitShellProvider>();
        private readonly CloudFormationViewer _cloudFormationViewer;

        public CloudFormationViewerTests()
        {
            _cloudFormationViewer = new CloudFormationViewer(_shellProvider.Object, null);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void View(string stackName)
        {
            Assert.Throws<ArgumentException>(() => _cloudFormationViewer.View(stackName, null, null));
        }
    }
}
