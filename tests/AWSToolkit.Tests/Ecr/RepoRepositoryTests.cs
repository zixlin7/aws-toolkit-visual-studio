using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ECS.PluginServices.Ecr;
using Amazon.ECR;
using Amazon.ECR.Model;

using Moq;

using Xunit;

using EcrRepository = Amazon.ECR.Model.Repository;

namespace AWSToolkit.Tests.Ecr
{
    public class RepoRepositoryTests
    {
        private readonly RepoRepository _sut;
        private readonly Mock<IAmazonECR> _ecrClient = new Mock<IAmazonECR>();

        private readonly List<EcrRepository> _sdkRepositories = new List<EcrRepository>();

        public RepoRepositoryTests()
        {
            _sut = new RepoRepository(_ecrClient.Object);

            SetupEcrClient();
            _sdkRepositories.Add(CreateSampleRepo());
            _sdkRepositories.Add(CreateSampleRepo());
            _sdkRepositories.Add(CreateSampleRepo());
        }

        [Fact]
        public async Task GetRepositoriesAsync()
        {
            var repos = await _sut.GetRepositoriesAsync();
            Assert.Equal(_sdkRepositories.Count, repos.Count());
        }

        private void SetupEcrClient()
        {
            _ecrClient.Setup(mock =>
                    mock.DescribeRepositoriesAsync(It.IsAny<DescribeRepositoriesRequest>(),
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new DescribeRepositoriesResponse()
                {
                    NextToken = string.Empty, Repositories = _sdkRepositories,
                });
        }

        private EcrRepository CreateSampleRepo()
        {
            return new EcrRepository()
            {
                CreatedAt = DateTime.UtcNow,
                RepositoryArn = "some-arn",
                RepositoryName = "some-repo-name",
                RepositoryUri = "some-uri",
            };
        }
    }
}
