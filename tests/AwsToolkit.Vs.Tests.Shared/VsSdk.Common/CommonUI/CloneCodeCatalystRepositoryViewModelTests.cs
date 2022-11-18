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
        private readonly Mock<IFolderBrowserDialog> _folderDialog = new Mock<IFolderBrowserDialog>();
        private readonly ICredentialIdentifier _sampleIdentifier = new SonoCredentialIdentifier("sample");

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

#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            _sut = new CloneCodeCatalystRepositoryViewModel(_toolkitContextFixture.ToolkitContext, taskContext.Factory);
            _sampleConnectionState = new ConnectionState.ValidConnection(_sampleIdentifier, _sut.AwsIdRegion);
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


        [Fact]
        public void InvalidPathCharsCreateValidationErrorForLocalPath()
        {
            var info = (INotifyDataErrorInfo) _sut;

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

            using (var testLocation = new TemporaryTestLocation(false))
            {
                Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));

                _sut.LocalPath = testLocation.TestFolder;

                Assert.Empty(info.GetErrors(nameof(_sut.LocalPath)));
            }
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

        public void Dispose()
        {
            _sut?.Dispose();
        }
    }
}
