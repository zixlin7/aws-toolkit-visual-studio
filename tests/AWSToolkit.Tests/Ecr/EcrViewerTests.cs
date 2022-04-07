using System;
using System.Collections.Generic;

using Amazon;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.ECR;
using Amazon.ECR.Model;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Ecr
{
    public class EcrViewerTests
    {
        private static readonly ToolkitRegion SampleRegion = new ToolkitRegion()
        {
            DisplayName = "sample-region", Id = "sample-region",
        };

        private static readonly ICredentialIdentifier SampleCredentialId =
            FakeCredentialIdentifier.Create("sample-profile");

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly EcrViewer _sut;

        public EcrViewerTests()
        {
            _sut = new EcrViewer(_toolkitContextFixture.ToolkitContext);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ViewRepository_NoName(string repoName)
        {
            Assert.Throws<ArgumentException>(() => _sut.ViewRepository(repoName, null));
        }

        [Fact]
        public void ViewRepository()
        {
            string repoName = "some-repo";

            // Setup
            var client = new Mock<AmazonECRClient>(new AwsMockCredentials(), RegionEndpoint.USWest2);

            client.Setup(mock => mock.DescribeRepositories(It.IsAny<DescribeRepositoriesRequest>()))
                .Returns(new DescribeRepositoriesResponse()
                {
                    Repositories = new List<Repository>() { new Repository() { RepositoryName = repoName } }
                });

            _toolkitContextFixture.SetupCreateServiceClient<AmazonECRClient>(client.Object);

            // Act
            _sut.ViewRepository(repoName, new AwsConnectionSettings(SampleCredentialId, SampleRegion));

            // Assert (this would be the attempt to show the resource)
            _toolkitContextFixture.ToolkitHost.Verify(mock => mock.ExecuteOnUIThread(It.IsAny<Action>()), Times.Once);
        }
    }
}
