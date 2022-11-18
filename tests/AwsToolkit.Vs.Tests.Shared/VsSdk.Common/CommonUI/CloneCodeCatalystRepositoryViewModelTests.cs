using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCatalyst;
using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.SourceControl;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class CloneCodeCatalystRepositoryViewModelTests : IDisposable
    {
        private const string _regionId = "us-east-1";
        private const string _spaceName = "test-space-name";
        private const string _projectName = "test-project-name";
        private const string _repoName = "test-repo-name";

        private readonly CloneCodeCatalystRepositoryViewModel _sut;
        private readonly ConnectionState _sampleConnectionState;
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<IAWSCodeCatalyst> _codeCatalyst = new Mock<IAWSCodeCatalyst>();
        private readonly Mock<IGitService> _git = new Mock<IGitService>();
        private readonly Mock<IFolderBrowserDialog> _folderDialog = new Mock<IFolderBrowserDialog>();
        private readonly ICredentialIdentifier _sampleIdentifier = new SonoCredentialIdentifier("sample");
        private readonly TemporaryTestLocation _localPathTemporaryTestLocation = new TemporaryTestLocation(false);

        public CloneCodeCatalystRepositoryViewModelTests()
        {
            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(It.Is<Type>(value => typeof(IAWSCodeCatalyst).Equals(value))))
                .Returns(_codeCatalyst.Object);

            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.CreateProgressDialog()).ReturnsAsync(new FakeProgressDialog());

            _toolkitContextFixture.DefineRegion(new ToolkitRegion() { Id = _regionId });

            _codeCatalyst.Setup(mock => mock.GetSpacesAsync(It.IsAny<AwsConnectionSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<ICodeCatalystSpace>()
                {
                    new CodeCatalystSpace(_spaceName, "test-space-display-name", "Test space description.", _regionId)
                }.AsEnumerable()));

            _codeCatalyst.Setup(mock => mock.GetProjectsAsync(It.Is<string>(spaceName => _spaceName == spaceName), It.IsAny<AwsConnectionSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<ICodeCatalystProject>()
                {
                    new CodeCatalystProject(_projectName, _spaceName, "test-project-display-name", "Test project description.")
                }.AsEnumerable()));

            CloneUrlsFactoryAsync factory = repoName => Task.FromResult(new CloneUrls(new Uri("http://test")));

            _codeCatalyst.Setup(mock => mock.GetRemoteRepositoriesAsync(It.Is<string>(spaceName => _spaceName == spaceName), It.Is<string>(projectName => _projectName == projectName),
                It.IsAny<AwsConnectionSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new List<ICodeCatalystRepository>()
                {
                    new CodeCatalystRepository(factory, _repoName, _spaceName, _projectName, "Test repo description.")
                }.AsEnumerable()));
            
            _git.Setup(mock => mock.GetDefaultRepositoryPath()).Returns(_localPathTemporaryTestLocation.TestFolder);

#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            _sut = new CloneCodeCatalystRepositoryViewModel(_toolkitContextFixture.ToolkitContext, taskContext.Factory, _git.Object);
            _sampleConnectionState = new ConnectionState.ValidConnection(_sampleIdentifier, _sut.AwsIdRegion);
        }

        public void Dispose()
        {
            _localPathTemporaryTestLocation?.Dispose();
            _sut?.Dispose();
        }

        [Fact]
        public void UpdateConnectionSettings()
        {
            Assert.Null(_sut.ConnectionSettings);

            _sut.Connection.CredentialIdentifier = _sampleIdentifier;
            _sut.UpdateConnectionSettings();

            Assert.NotNull(_sut.ConnectionSettings);
            Assert.Equal(_sampleIdentifier.Id, _sut.ConnectionSettings.CredentialIdentifier.Id);
            Assert.Equal(_regionId, _sut.ConnectionSettings.Region.Id);
        }

        public static IEnumerable<object[]> InvalidConnectionState = new List<object[]>
        {
            new object[] { new ConnectionState.IncompleteConfiguration(null, null)},
            new object[] { new ConnectionState.InvalidConnection(null)},
            new object[] { new ConnectionState.ValidatingConnection() },
            new object[] { new ConnectionState.InitializingToolkit()},
        };

        [Theory]
        [MemberData(nameof(InvalidConnectionState))]
        public void UpdateSpacesForConnectionState_Invalid(ConnectionState state)
        {
            Assert.Empty(_sut.Spaces);

             _sut.UpdateSpacesForConnectionState(state);

             Assert.Empty(_sut.Spaces);
        }

        [Fact]
        public void UpdateSpacesForConnectionState_Valid()
        {
            Assert.Empty(_sut.Spaces);

            SetupInitialConnection();

            _sut.UpdateSpacesForConnectionState(_sampleConnectionState);

            Assert.NotEmpty(_sut.Spaces);
        }

        [Fact]
        public void SelectingSpaceRefreshesProjects()
        {
            Assert.Empty(_sut.Projects);

            SetupInitialSpaces();

            _sut.SelectedSpace = _sut.Spaces.First();

            Assert.NotEmpty(_sut.Projects);
        }

        [Fact]
        public void SelectingProjectRefreshesRepositories()
        {
            Assert.Empty(_sut.Repositories);

            SetupInitialSpaces();

            _sut.SelectedSpace = _sut.Spaces.First();
            _sut.SelectedProject = _sut.Projects.First();

            Assert.NotEmpty(_sut.Repositories);
        }

        [Fact]
        public void ExecuteBrowseForRepositoryPathCommandUpdatesLocalPathOnOk()
        {
            var initialPath = _sut.LocalPath;
            var expected = @"c:\test\path";

            _folderDialog.Setup(mock => mock.FolderPath).Returns(expected);
            _folderDialog.Setup(mock => mock.ShowModal()).Returns(true);
            _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateFolderBrowserDialog()).Returns(_folderDialog.Object);

            Assert.Equal(initialPath, _sut.LocalPath);

            _sut.BrowseForRepositoryPathCommand.Execute(null);

            Assert.Equal(expected, _sut.LocalPath);
        }

        [Fact]
        public void ExecuteBrowseForRepositoryPathCommandDoesNotUpdateLocalPathOnCancel()
        {
            var expected = _sut.LocalPath;

            _folderDialog.Setup(mock => mock.ShowModal()).Returns(false);
            _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateFolderBrowserDialog()).Returns(_folderDialog.Object);

            Assert.Equal(expected, _sut.LocalPath);

            _sut.BrowseForRepositoryPathCommand.Execute(null);

            Assert.Equal(expected, _sut.LocalPath);
        }

        [Fact]
        public void InvalidPathCharsCreateValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;
            _sut.SelectedRepository = new Mock<ICodeCatalystRepository>().Object;

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));

            // This is a best effort test, but even MS states in the docs "...this method is not guaranteed to contain the complete
            // set of characters that are invalid in file and directory names. The full set of invalid characters can vary by file system."
            // https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getinvalidpathchars?view=netframework-4.7.2#remarks
            _sut.LocalPath = new string(Path.GetInvalidPathChars());

            Assert.NotEmpty(info.GetErrors(nameof(_sut.LocalPath)));
        }

        [Fact]
        public void NonExistingDirectoryDoesNotCreateValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));

            _sut.LocalPath = Guid.NewGuid().ToString();

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));
        }

        [Fact]
        public void EmptyExistingDirectoryDoesNotCreateValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));
        }

        [Fact]
        public void NotEmptyExistingDirectoryCreatesValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;

            using (var testLocation = new TemporaryTestLocation())
            {
                Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));

                _sut.LocalPath = testLocation.TestFolder;

                Assert.NotEmpty(info.GetErrors(nameof(_sut.LocalPath)));
            }
        }

        [Fact]
        public void LocalPathHasDefaultValue()
        {
            Assert.NotEmpty(_sut.LocalPath);
        }

        private Mock<ICodeCatalystRepository> MockCodeCatalystRepository(string repoName)
        {
            Mock<ICodeCatalystRepository> mock = new Mock<ICodeCatalystRepository>();
            mock.SetupGet(m => m.Name).Returns(repoName);

            return mock;
        }

        [Fact]
        public void OnlyAddsSelectedRepositoryNameToLocalPathOnFirstSelection()
        {
            var selectedRepoName = "TestRepo";
            var expected = Path.Combine(_sut.LocalPath, selectedRepoName);

            _sut.SelectedRepository = MockCodeCatalystRepository(selectedRepoName).Object;

            Assert.Equal(expected, _sut.LocalPath);
        }

        [Fact]
        public void ReplacesRepositoryNameAtEndOfLocalPathOnSelectionChange()
        {
            var selectedRepoName1 = "TestRepo";
            var selectedRepoName2 = "AndNowForSomethingCompletelyDifferent";
            var expected = Path.Combine(_sut.LocalPath, selectedRepoName2);

            _sut.SelectedRepository = MockCodeCatalystRepository(selectedRepoName1).Object;
            _sut.SelectedRepository = MockCodeCatalystRepository(selectedRepoName2).Object;

            Assert.Equal(expected, _sut.LocalPath);
        }

        [Fact]
        public void SelectingRepositoryOnEmptyLocalPathAddsRepositoryNameOnly()
        {
            var selectedRepoName = "TestRepo";
            var expected = Path.Combine(_localPathTemporaryTestLocation.TestFolder, selectedRepoName);

            _sut.LocalPath = string.Empty;
            _sut.SelectedRepository = MockCodeCatalystRepository(selectedRepoName).Object;

            Assert.Equal(expected, _sut.LocalPath);
        }

        [Fact]
        public void RepositoryNameIsOnlyAppendedWhenPreviousRepositoryNameInLocalPathWasEdited()
        {
            var selectedRepoName1 = "TestRepo";
            var selectedRepoName2 = "AndNowForSomethingCompletelyDifferent";
            var spoiler = "hahaha";
            var expected = Path.Combine(_sut.LocalPath, selectedRepoName1 + spoiler);

            _sut.SelectedRepository = MockCodeCatalystRepository(selectedRepoName1).Object;
            _sut.LocalPath += spoiler;
            _sut.SelectedRepository = MockCodeCatalystRepository(selectedRepoName2).Object;

            Assert.Equal(expected, _sut.LocalPath);
        }

        private void SetupInitialSpaces()
        {
            SetupInitialConnection();
            _sut.UpdateSpacesForConnectionState(_sampleConnectionState);
        }

        private void SetupInitialConnection()
        {
            _sut.Connection.CredentialIdentifier = _sampleIdentifier;
            _sut.UpdateConnectionSettings();
            _sut.Connection.IsConnectionValid = true;
        }
    }
}
