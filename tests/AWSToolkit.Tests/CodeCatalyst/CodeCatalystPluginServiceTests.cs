using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.CodeCatalyst;
using Amazon.CodeCatalyst.Model;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CodeCatalyst
{
    public class CodeCatalystPluginServiceTests
    {
        private const string _regionId = "us-west-2";
        private static readonly RegionEndpoint _regionEndpoint = RegionEndpoint.USWest2;

        private readonly CodeCatalystPluginService _sut;
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<AmazonCodeCatalystClient> _client = new Mock<AmazonCodeCatalystClient>(new AwsMockCredentials(), _regionEndpoint);

        private readonly AwsConnectionSettings _settings = ConnectionManagerFixture.CreateAwsConnectionSettings();

        public CodeCatalystPluginServiceTests()
        {
            _toolkitContextFixture.SetupGetToolkitCredentials(new ToolkitCredentials(new FakeCredentialIdentifier(), new FakeTokenProvider()));
            _toolkitContextFixture.SetupCreateServiceClient(_client.Object);

            _sut = new CodeCatalystPluginService(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public async Task GetSpacesAsyncReturnsValues()
        {
            const string name = "test-space-name";
            const string displayName = "test-space-display-name";
            const string description = "Test space description.";

            var space = new CodeCatalystSpace(name, displayName, description, _regionId);

            _client.Setup(
                mock => mock.ListSpacesAsync(It.IsAny<ListSpacesRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ListSpacesResponse()
                {
                    Items = new List<SpaceSummary>() {
                        new SpaceSummary()
                        {
                            Name = name,
                            DisplayName = displayName,
                            Description = description,
                            RegionName = _regionId
                        }
                    }
                }));
            ;

            var spaces = new List<ICodeCatalystSpace>(await _sut.GetSpacesAsync(_settings));

            Assert.Single(spaces);
            Assert.Contains(space, spaces);
        }

        [Fact]
        public async Task GetProjectsAsyncReturnsValues()
        {
            const string name = "test-project-name";
            const string spaceName = "test-space-name";
            const string displayName = "test-project-display-name";
            const string description = "Test project description.";

            var project = new CodeCatalystProject(name, spaceName, displayName, description);

            _client.Setup(
                mock => mock.ListProjectsAsync(It.IsAny<ListProjectsRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ListProjectsResponse()
                {
                    Items = new List<ProjectSummary>() {
                        new ProjectSummary()
                        {
                            Name = name,
                            DisplayName = displayName,
                            Description = description
                        }
                    }
                }));

            var projects = new List<ICodeCatalystProject>(await _sut.GetProjectsAsync(spaceName, _settings));

            Assert.Single(projects);
            Assert.Contains(project, projects);
        }

        [Fact]
        public async Task GetRemoteRepositoriesReturnsValues()
        {
            const string name = "test-repo-name";
            const string spaceName = "test-space-name";
            const string projectName = "test-project-name";
            const string description = "Test repo description.";

            CloneUrlsFactoryAsync factory = repoName => Task.FromResult(new CloneUrls(new Uri("https://test")));

            var repo = new CodeCatalystRepository(factory, name, spaceName, projectName, description);

            _client.Setup(
                mock => mock.ListSourceRepositoriesAsync(It.IsAny<ListSourceRepositoriesRequest>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new ListSourceRepositoriesResponse()
                {
                    Items = new List<ListSourceRepositoriesItem>() {
                        new ListSourceRepositoriesItem()
                        {
                            Name = name,
                            Description = description
                        }
                    }
                }));

            var repos = new List<ICodeCatalystRepository>(await _sut.GetRemoteRepositoriesAsync(spaceName, projectName, _settings));

            Assert.Single(repos);
            Assert.Contains(repo, repos);
        }
    }
}
