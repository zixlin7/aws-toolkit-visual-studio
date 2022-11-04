using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class CloneCodeCatalystRepositoryViewModelTests
    {
        private const string _regionId = "us-east-1";
        private const string _spaceName = "test-space-name";
        private const string _projectName = "test-project-name";
        private const string _repoName = "test-repo-name";

        private readonly CloneCodeCatalystRepositoryViewModel _sut;
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<IAWSCodeCatalyst> _codeCatalyst = new Mock<IAWSCodeCatalyst>();
        private readonly SonoCredentialProviderFactory _credentialFactory;
        private readonly Mock<IFolderBrowserDialog> _folderDialog = new Mock<IFolderBrowserDialog>();

        public CloneCodeCatalystRepositoryViewModelTests()
        {
            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(It.Is<Type>(value => typeof(IAWSCodeCatalyst).Equals(value))))
                .Returns(_codeCatalyst.Object);

            _toolkitContextFixture.DefineRegion(new ToolkitRegion() { Id = _regionId });

            _codeCatalyst.Setup(mock => mock.GetSpacesAsync(It.IsAny<AwsConnectionSettings>()))
                .Returns(Task.FromResult(new List<ICodeCatalystSpace>()
                {
                    new CodeCatalystSpace(_spaceName, "test-space-display-name", "Test space description.", _regionId)
                }.AsEnumerable()));

            _codeCatalyst.Setup(mock => mock.GetProjectsAsync(It.Is<string>(spaceName => _spaceName == spaceName), It.IsAny<AwsConnectionSettings>()))
                .Returns(Task.FromResult(new List<ICodeCatalystProject>()
                {
                    new CodeCatalystProject(_projectName, _spaceName, "test-project-display-name", "Test project description.")
                }.AsEnumerable()));

            CloneUrlsFactoryAsync factory = repoName => Task.FromResult(new CloneUrls(new Uri("http://test")));

            _codeCatalyst.Setup(mock => mock.GetRemoteRepositoriesAsync(It.Is<string>(spaceName => _spaceName == spaceName), It.Is<string>(projectName => _projectName == projectName), It.IsAny<AwsConnectionSettings>()))
                .Returns(Task.FromResult(new List<ICodeCatalystRepository>()
                {
                    new CodeCatalystRepository(factory, _repoName, _spaceName, _projectName, "Test repo description.")
                }.AsEnumerable()));

#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            _credentialFactory = new SonoCredentialProviderFactory(null);
            _credentialFactory.Initialize();

            _sut = new CloneCodeCatalystRepositoryViewModel(_toolkitContextFixture.ToolkitContext, taskContext.Factory);
        }

        [Fact]
        public void ConnectionSettingsIsSetWhenSelectedCredentialChanges()
        {
            Assert.Null(_sut.ConnectionSettings);

            _sut.SelectedCredential = _sut.Credentials.First();

            Assert.True(_credentialFactory.Supports(_sut.ConnectionSettings.CredentialIdentifier, AwsConnectionType.AwsToken));
            Assert.Equal(_regionId, _sut.ConnectionSettings.Region.Id);
        }

        [Fact]
        public void SelectingCredentialRefreshesSpaces()
        {
            Assert.Empty(_sut.Spaces);

            _sut.SelectedCredential = _sut.Credentials.First();

            Assert.NotEmpty(_sut.Spaces);
        }

        [Fact]
        public void SelectingSpaceRefreshesProjects()
        {
            Assert.Empty(_sut.Projects);

            _sut.SelectedCredential = _sut.Credentials.First();
            _sut.SelectedSpace = _sut.Spaces.First();

            Assert.NotEmpty(_sut.Projects);
        }

        [Fact]
        public void SelectingProjectRefreshesRepositories()
        {
            Assert.Empty(_sut.Repositories);

            _sut.SelectedCredential = _sut.Credentials.First();
            _sut.SelectedSpace = _sut.Spaces.First();
            _sut.SelectedProject = _sut.Projects.First();

            Assert.NotEmpty(_sut.Repositories);
        }

        [Fact]
        public void ExecuteBrowseForRepositoryPathCommandUpdatesLocalPathOnOk()
        {
            const string expectedPath = @"c:\test\path";

            _folderDialog.Setup(mock => mock.FolderPath).Returns(expectedPath);
            _folderDialog.Setup(mock => mock.ShowModal()).Returns(true);
            _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateFolderBrowserDialog()).Returns(_folderDialog.Object);

            Assert.Null(_sut.LocalPath);

            _sut.BrowseForRepositoryPathCommand.Execute(null);

            Assert.Equal(expectedPath, _sut.LocalPath);
        }

        [Fact]
        public void ExecuteBrowseForRepositoryPathCommandDoesNotUpdateLocalPathOnCancel()
        {
            _folderDialog.Setup(mock => mock.ShowModal()).Returns(false);
            _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateFolderBrowserDialog()).Returns(_folderDialog.Object);

            Assert.Null(_sut.LocalPath);

            _sut.BrowseForRepositoryPathCommand.Execute(null);

            Assert.Null(_sut.LocalPath);
        }

        // TODO IDE-8848 Be sure to add tests once LocalPath has validations
    }
}
