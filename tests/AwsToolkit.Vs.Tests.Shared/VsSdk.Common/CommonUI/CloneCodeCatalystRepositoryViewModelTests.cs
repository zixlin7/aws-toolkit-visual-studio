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
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly Mock<IAWSCodeCatalyst> _codeCatalyst = new Mock<IAWSCodeCatalyst>();
        private readonly Mock<IGitService> _git = new Mock<IGitService>();
        private readonly Mock<IFolderBrowserDialog> _folderDialog = new Mock<IFolderBrowserDialog>();
        private readonly ICredentialIdentifier _sampleIdentifier = new SonoCredentialIdentifier("sample");
        private readonly TemporaryTestLocation _localPathTemporaryTestLocation = new TemporaryTestLocation(false);
        private readonly ToolkitRegion _sampleRegion = new ToolkitRegion() { Id = _regionId };

        public CloneCodeCatalystRepositoryViewModelTests()
        {
            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.QueryAWSToolkitPluginService(It.Is<Type>(value => typeof(IAWSCodeCatalyst).Equals(value))))
                .Returns(_codeCatalyst.Object);

            _toolkitContextFixture.ToolkitHost.Setup(mock => mock.CreateProgressDialog()).ReturnsAsync(new FakeProgressDialog());

            _toolkitContextFixture.DefineRegion(_sampleRegion);

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

            CloneUrlsFactoryAsync factory = repoName => Task.FromResult(new CloneUrls(new Uri($"https://codecatalyst.aws/{repoName}")));
            var repo = new CodeCatalystRepository(factory, _repoName, _spaceName, _projectName,
                "Test repo description.");
            SetupGetRemoteRepositories(new List<ICodeCatalystRepository>{ repo });

            _git.Setup(mock => mock.GetDefaultRepositoryPath()).Returns(_localPathTemporaryTestLocation.TestFolder);

#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            _sut = new CloneCodeCatalystRepositoryViewModel(_toolkitContextFixture.ToolkitContext, taskContext.Factory, _git.Object);
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

        [Fact]
        public void ApplyConnectionState_ValidConnection()
        {
            // Force a unique username back -- we don't have to set up the connection manager to actually validate a connection in this test.
            var userName = Guid.NewGuid().ToString();
            _codeCatalyst.Setup(mock => mock.GetUserNameAsync(It.IsAny<string>(), It.IsAny<AwsConnectionSettings>()))
                .ReturnsAsync(userName);

            SetupInitialSpaces();

            _sut.ApplyConnectionState(new ConnectionState.ValidConnection(_sampleIdentifier, _sampleRegion));

            Assert.Equal(userName, _sut.ConnectionStatus.Text);
        }

        [Fact]
        public void ApplyConnectionState_Validating()
        {
            SetupInitialSpaces();

            _sut.ApplyConnectionState(new ConnectionState.ValidatingConnection());

            Assert.Contains("Connecting", _sut.ConnectionStatus.Text);
        }

        [Fact]
        public void ApplyConnectionState_InvalidConnection()
        {
            string validationMessage = "some validation failure";

            _sut.ApplyConnectionState(new ConnectionState.InvalidConnection(validationMessage));

            Assert.Contains("Not Connected", _sut.ConnectionStatus.Text);
            Assert.Contains(validationMessage, _sut.ConnectionFailure);
        }

        [Fact]
        public void ExecuteLogin()
        {
            Assert.Null(_sut.Identifier);

            _sut.LoginCommand.Execute(null);

            Assert.NotNull(_sut.Identifier);
            Assert.Equal(SonoCredentialProviderFactory.FactoryId, _sut.Identifier.FactoryId);
        }

        [Fact]
        public void ExecuteLogout()
        {
            var identifier = new SonoCredentialIdentifier(SonoCredentialProviderFactory.CodeCatalystProfileName);
            _sut.Connection.CredentialIdentifier = identifier;

            _sut.LogoutCommand.Execute(null);

            _toolkitContextFixture.CredentialManager.Verify(
                mock => mock.Invalidate(It.Is<ICredentialIdentifier>(credentialId =>
                    credentialId.Id.Equals(identifier.Id))), Times.Once);
            Assert.Null(_sut.Identifier);
        }

        public static IEnumerable<object[]> CanExecuteData = new List<object[]>
        {
            new object[] { new SonoCredentialIdentifier(SonoCredentialProviderFactory.CodeCatalystProfileName), true},
            new object[] { new SonoCredentialIdentifier("sample"), false},
            new object[] { new SharedCredentialIdentifier("sample"), false },
            new object[] { null, false},
        };

        [Theory]
        [MemberData(nameof(CanExecuteData))]
        public void CanExecuteLogout(ICredentialIdentifier identifier, bool expectedResult)
        {
            _sut.Connection.CredentialIdentifier = identifier;

            var result = _sut.LogoutCommand.CanExecute(null);

            Assert.Equal(expectedResult, result);
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
        public void SettingPathRaisesSubmitCanExecute()
        {
            Assert.False(_sut.SubmitDialogCommand.CanExecute(null));

            SetupInitialConnection();
            SetupInitialRepository();

            var eventRaised = false;
            void Handler(object obj, EventArgs eventArgs) => eventRaised = true;

            _sut.SubmitDialogCommand.CanExecuteChanged += Handler;
            _sut.LocalPath = "some-value";

            Assert.True(eventRaised);
            _sut.SubmitDialogCommand.CanExecuteChanged -= Handler;
        }

        [Fact]
        public void CancelDialogsSetDialogResultToFalse()
        {
            Assert.Null(_sut.DialogResult);

            _sut.CancelDialogCommand.Execute(null);

            Assert.False(_sut.DialogResult);
        }

        [Fact]
        public void SubmitDialogSetsDialogResultToTrue()
        {
            Assert.Null(_sut.DialogResult);

            SetupInitialConnection();
            SetupInitialRepository();

            _sut.SubmitDialogCommand.Execute(null);

            Assert.True(_sut.DialogResult);
        }

        public static IEnumerable<object[]> InvalidPathSetup = new List<object[]>
        {
            new object[] { false, new Mock<ICodeCatalystRepository>().Object},
            new object[] { true, null},
            new object[] { false, null},
        };

        [Theory]
        [MemberData(nameof(InvalidPathSetup))]
        public void ValidatePathDoesNotSetErrorWhenInvalidSetup(bool isConnectionValid, ICodeCatalystRepository repository)
        {
            var info = (INotifyDataErrorInfo) _sut;
            _sut.Connection.IsConnectionValid = isConnectionValid;
            _sut.SelectedRepository = repository;

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));

            _sut.LocalPath = _localPathTemporaryTestLocation.TestFolder;

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));
        }

        [Fact]
        public void InvalidPathCharsCreateValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;
            SetupInitialRepository();

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
            SetupInitialRepository();

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));

            _sut.LocalPath = Guid.NewGuid().ToString();

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));
        }

        [Fact]
        public void EmptyExistingDirectoryDoesNotCreateValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;
            SetupInitialRepository();

            Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));
        }

        [Fact]
        public void NotEmptyExistingDirectoryCreatesValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;
            SetupInitialRepository();

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


        [Fact]
        public void FiltersThirdPartyRepos()
        {
            var firstPartyRepo = MockCodeCatalystRepository("code-catalyst", "https://codecatalyst.aws/code-catalyst");
            var thirdPartyRepo =
                MockCodeCatalystRepository("not-code-catalyst", "https://github.com/aws/not-code-catalyst.git");

            SetupGetRemoteRepositories(
                new List<ICodeCatalystRepository> { firstPartyRepo.Object, thirdPartyRepo.Object });

            Assert.Empty(_sut.Repositories);

            SetupInitialSpaces();

            _sut.SelectedSpace = _sut.Spaces.First();
            _sut.SelectedProject = _sut.Projects.First();

            Assert.Single(_sut.Repositories);
            Assert.Contains(firstPartyRepo.Object, _sut.Repositories);
        }


        private void SetupInitialSpaces()
        {
            SetupInitialConnection();
            _sut.RefreshSpaces();
        }

        private void SetupInitialConnection()
        {
            _sut.Connection.CredentialIdentifier = _sampleIdentifier;
            _sut.UpdateConnectionSettings();
            _sut.Connection.IsConnectionValid = true;
        }

        private void SetupInitialRepository()
        {
            _sut.Connection.IsConnectionValid = true;
            _sut.SelectedRepository = new Mock<ICodeCatalystRepository>().Object;
        }

        private void SetupGetRemoteRepositories(List<ICodeCatalystRepository> repos)
        {
            _codeCatalyst.Setup(mock => mock.GetRemoteRepositoriesAsync(
                    It.Is<string>(spaceName => _spaceName == spaceName),
                    It.Is<string>(projectName => _projectName == projectName),
                    It.IsAny<AwsConnectionSettings>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(repos.AsEnumerable()));
        }

        private Mock<ICodeCatalystRepository> MockCodeCatalystRepository(string repoName)
        {
            var mock = new Mock<ICodeCatalystRepository>();
            mock.SetupGet(m => m.Name).Returns(repoName);

            return mock;
        }

        private Mock<ICodeCatalystRepository> MockCodeCatalystRepository(string repoName, string cloneUrl)
        {
            var mock = MockCodeCatalystRepository(repoName);
            mock.Setup(x => x.GetCloneUrlAsync(It.IsAny<CloneUrlType>()))
                .ReturnsAsync(new Uri(cloneUrl));

            return mock;
        }
    }
}
