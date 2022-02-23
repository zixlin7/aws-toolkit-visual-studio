using System;

using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.Shared;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class BeanstalkEnvironmentViewerTests
    {
        private readonly Mock<IAWSToolkitShellProvider> _shellProvider = new Mock<IAWSToolkitShellProvider>();
        private readonly BeanstalkEnvironmentViewer _environmentViewer;

        public BeanstalkEnvironmentViewerTests()
        {
            _environmentViewer = new BeanstalkEnvironmentViewer(_shellProvider.Object, null);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void View(string environmentName)
        {
            Assert.Throws<ArgumentException>(() => _environmentViewer.View(environmentName, null, null));
        }
    }
}
