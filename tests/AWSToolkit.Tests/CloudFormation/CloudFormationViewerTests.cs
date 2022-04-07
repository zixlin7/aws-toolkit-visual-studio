using System;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CloudFormation
{
    public class CloudFormationViewerTests
    {
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly CloudFormationViewer _cloudFormationViewer;

        public CloudFormationViewerTests()
        {
            _cloudFormationViewer = new CloudFormationViewer(_toolkitContextFixture.ToolkitContext);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void View(string stackName)
        {
            Assert.Throws<ArgumentException>(() => _cloudFormationViewer.View(stackName, null));
        }
    }
}
