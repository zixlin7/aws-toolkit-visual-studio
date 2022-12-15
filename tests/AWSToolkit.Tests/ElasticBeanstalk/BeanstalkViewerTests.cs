using System;
using System.Linq;

using Amazon;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ElasticBeanstalk;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class BeanstalkViewerTests
    {
        private static readonly string SampleEnvironmentName = "some-environment";
        private static readonly ToolkitRegion SampleRegion = new ToolkitRegion()
        {
            DisplayName = "sample-region",
            Id = "sample-region",
        };

        private static readonly ICredentialIdentifier SampleCredentialId =
            FakeCredentialIdentifier.Create("sample-profile");

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly Mock<AmazonElasticBeanstalkClient> _beanstalkClient =
            new Mock<AmazonElasticBeanstalkClient>(new AwsMockCredentials(), RegionEndpoint.USWest2);

        private readonly AwsConnectionSettings _connectionSettings =
            new AwsConnectionSettings(SampleCredentialId, SampleRegion);

        private readonly BeanstalkViewer _sut;

        public BeanstalkViewerTests()
        {
            _toolkitContextFixture.SetupCreateServiceClient<AmazonElasticBeanstalkClient>(_beanstalkClient.Object);
            _sut = new BeanstalkViewer(_toolkitContextFixture.ToolkitContext);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ViewEnvironment_NoName(string environmentName)
        {
            Assert.Throws<ArgumentException>(() => _sut.ViewEnvironment(environmentName, null));
        }

        [Fact]
        public void ViewEnvironment()
        {
            // Setup
            SetupDescribeEnvironments(1);

            // Act
            _sut.ViewEnvironment(SampleEnvironmentName, _connectionSettings);

            // Assert (this would be the attempt to show the resource)
            _toolkitContextFixture.ToolkitHost.Verify(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()), Times.Once);
        }

        [Theory]
        [InlineData(0, BeanstalkViewerExceptionCode.EnvironmentNotFound)]
        [InlineData(2, BeanstalkViewerExceptionCode.TooManyEnvironments)]
        public void ViewEnvironment_LoadsNonSingleEnvironmentList(int returnedEnvironmentCount, BeanstalkViewerExceptionCode expectedErrorCode)
        {
            SetupDescribeEnvironments(returnedEnvironmentCount);

            var exception = Assert.Throws<BeanstalkViewerException>(() => _sut.ViewEnvironment(SampleEnvironmentName, _connectionSettings));
            Assert.Equal(expectedErrorCode, exception.ErrorCode);
        }

        private void SetupDescribeEnvironments(int returnedEnvironmentCount)
        {
            var environmentList = Enumerable.Range(0, returnedEnvironmentCount)
                .Select(_ => new EnvironmentDescription() { EnvironmentName = SampleEnvironmentName })
                .ToList();

            _beanstalkClient.Setup(mock => mock.DescribeEnvironments(It.IsAny<DescribeEnvironmentsRequest>()))
                .Returns(new DescribeEnvironmentsResponse() { Environments = environmentList, });
        }
    }
}
