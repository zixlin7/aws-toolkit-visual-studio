using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.CodeCatalyst.Model;

using Xunit;

namespace AWSToolkit.Tests.CodeCatalyst.Models
{
    public class CodeCatalystRepositoryTests
    {
        private const string _httpsCloneUrlString = "https://user@no.where/my-space/my-project/test-name";
        private readonly Uri _httpsCloneUrl;
        private readonly CloneUrls _cloneUrls;
        private readonly CloneUrlsFactoryAsync _cloneUrlsFactoryAsync;

        private const string _name = "test-name";
        private const string _spaceName = "my-space";
        private const string _projectName = "my-project";
        private const string _description = "Description for testing.\nBlah blah blah\nyada yada yada";

        private readonly CodeCatalystRepository _sut;

        public CodeCatalystRepositoryTests()
        {
            _httpsCloneUrl = new Uri(_httpsCloneUrlString);
            _cloneUrls = new CloneUrls(_httpsCloneUrl);
            _cloneUrlsFactoryAsync = repoName => Task.FromResult(_cloneUrls);

            _sut = new CodeCatalystRepository(_cloneUrlsFactoryAsync, _name, _spaceName, _projectName, _description);
        }

        [Fact]
        public void PropertiesReflectCtorWithPrimitiveArgs()
        {
            Assert.Equal(_name, _sut.Name);
            Assert.Equal(_spaceName, _sut.SpaceName);
            Assert.Equal(_projectName, _sut.ProjectName);
            Assert.Equal(_description, _sut.Description);
        }

        [Fact]
        public void PropertiesReflectCtorWithAwsSdkArgs()
        {
            var listSourceRepositoriesItem = new ListSourceRepositoriesItem()
            {
                Name = _name,
                Description = _description,
            };

            var sut = new CodeCatalystRepository(_cloneUrlsFactoryAsync, _spaceName, _projectName, listSourceRepositoriesItem);

            Assert.Equal(_name, sut.Name);
            Assert.Equal(_spaceName, sut.SpaceName);
            Assert.Equal(_projectName, sut.ProjectName);
            Assert.Equal(_description, sut.Description);
        }

        [Fact]
        public async Task GetCloneUrlUsesFactoryToReturnUrl()
        {
            var url = await _sut.GetCloneUrlAsync(CloneUrlType.Https);

            Assert.Equal(_httpsCloneUrl, url);
        }

        [Fact]
        public async Task GetCloneUrlDoesNotFailOnAnyCloneUrlType()
        {
            foreach (var cloneUrlType in (CloneUrlType[]) Enum.GetValues(typeof(CloneUrlType)))
            {
                // Just be sure an exception isn't thrown
                await _sut.GetCloneUrlAsync(cloneUrlType);
            }
        }
    }
}
