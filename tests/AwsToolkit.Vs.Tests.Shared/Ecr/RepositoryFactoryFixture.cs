using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS.Models.Ecr;
using Amazon.AWSToolkit.ECS.PluginServices.Ecr;
using Amazon.AWSToolkit.Regions;

using Moq;

namespace AwsToolkit.Vs.Tests.Ecr
{
    public class RepositoryFactoryFixture
    {
        public Mock<IRepositoryFactory> RepositoryFactory = new Mock<IRepositoryFactory>();
        public Mock<IRepoRepository> RepoRepository = new Mock<IRepoRepository>();

        public RepositoryFactoryFixture()
        {
            SetupRepositoryFactory();
        }

        private void SetupRepositoryFactory()
        {
            RepositoryFactory.Setup(mock =>
                    mock.CreateRepoRepository(It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>()))
                .Returns(RepoRepository.Object);
        }

        public void SetupGetRepositoriesAsync(IEnumerable<Repository> repositories)
        {
            RepoRepository.Setup(mock => mock.GetRepositoriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(repositories);
        }

        public IEnumerable<Repository> CreateSampleRepositories(int count)
        {
            return Enumerable.Range(0, count)
                .ToList()
                .Select(i => new Repository()
                {
                    Name = $"sample-repo-{i}",
                    Arn = $"sample-arn-{i}",
                    Uri = $"sample-uri-{i}",
                    CreatedOn = DateTime.UtcNow.AddDays(-i),
                });
        }
    }
}
