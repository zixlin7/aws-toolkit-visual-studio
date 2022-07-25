using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ECS.Models.Ecr;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using AwsToolkit.Vs.Tests.Ecr;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class RepositorySelectionViewModelTests
    {
        private readonly RepositorySelectionViewModel _sut;
        private readonly RepositoryFactoryFixture _repositoryFactoryFixture = new RepositoryFactoryFixture();

        private readonly FakeCredentialIdentifier _sampleCredentialsId = FakeCredentialIdentifier.Create("fake-profile");
        private readonly ToolkitRegion _sampleToolkitRegion = new ToolkitRegion();

        public RepositorySelectionViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            _sut = new RepositorySelectionViewModel(_repositoryFactoryFixture.RepositoryFactory.Object, taskContext.Factory);
            _sut.CredentialsId = _sampleCredentialsId;
            _sut.Region = _sampleToolkitRegion;
        }

        [StaFact]
        public async Task RefreshRepositoriesAsync()
        {
            int repoCount = 10;
            var sampleRepositories = _repositoryFactoryFixture.CreateSampleRepositories(repoCount).ToList();
            _repositoryFactoryFixture.SetupGetRepositoriesAsync(sampleRepositories);

            await _sut.RefreshRepositoriesAsync();

            Assert.Equal(repoCount, _sut.Repositories.Count);
            Assert.Equal(sampleRepositories, _sut.Repositories);
        }

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("hello", "he")]
        [InlineData("hello", "l")]
        [InlineData("hello", "H")]
        [InlineData("Hello", "h")]
        public void IsObjectFiltered_Match(string candidate, string filter)
        {
            var repo = new Repository() { Name = candidate, };
            Assert.True(RepositorySelectionViewModel.IsObjectFiltered(repo, filter));
        }

        [Theory]
        [InlineData("hello", "hello!")]
        [InlineData("hello", "x")]
        [InlineData("hello", "Q")]
        public void IsObjectFiltered_NoMatch(string candidate, string filter)
        {
            var repo = new Repository() { Name = candidate, };
            Assert.False(RepositorySelectionViewModel.IsObjectFiltered(repo, filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void IsObjectFiltered_NoFilter(string filter)
        {
            var repo = new Repository() { Name = "hello world", };
            Assert.True(RepositorySelectionViewModel.IsObjectFiltered(repo, filter));
        }

        [Theory]
        [InlineData("foo")]
        [InlineData(null)]
        [InlineData(3)]
        [InlineData(false)]
        public void IsObjectFiltered_NonRepository(object candidate)
        {
            Assert.False(RepositorySelectionViewModel.IsObjectFiltered(candidate, "3"));
        }
    }
}
