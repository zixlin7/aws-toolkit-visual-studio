using System;

using Amazon.AWSToolkit.ECS.Models.Ecr;

using Xunit;

using EcrRepository = Amazon.ECR.Model.Repository;

namespace AWSToolkit.Tests.Ecr
{
    public class RepositoryConversionTests
    {
        private readonly EcrRepository _samplerRepository = new EcrRepository()
        {
            CreatedAt = new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc),
            RepositoryArn = "some-arn",
            RepositoryName = "some-name",
            RepositoryUri = "some-uri",
        };

        [Fact]
        public void AsRepository()
        {
            var repository = _samplerRepository.AsRepository();
            Assert.Equal(_samplerRepository.CreatedAt.ToLocalTime(), repository.CreatedOn);
            Assert.Equal(_samplerRepository.RepositoryArn, repository.Arn);
            Assert.Equal(_samplerRepository.RepositoryName, repository.Name);
            Assert.Equal(_samplerRepository.RepositoryUri, repository.Uri);
        }
    }
}
