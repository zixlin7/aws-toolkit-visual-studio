using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;

using AWSToolkit.Tests.Credentials.Core;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles
{
    /// <summary>
    /// Unit tests for CredentialProfileFormViewModel.
    /// </summary>
    /// <remarks>
    /// Subform loading by SelectedCredentialType is handled entirely within XAML and not tested here.
    /// </remarks>
    public class CredentialProfileFormViewModelTests
    {
        private const string _accessKey = "ACCESSKEY4THETESTYAY"; // 20 chars starting with an "A" is typical

        private const string _secretKey = "aaaaaabbbbbbbbccccccccddddddddeeeeeeeffffffgggggghhhhh";

        private readonly CredentialProfileFormViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public CredentialProfileFormViewModelTests()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            _toolkitContextFixture.ToolkitContext.ConnectionManager = connectionManagerMock.Object;

            _sut = new CredentialProfileFormViewModel(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public async Task SaveValidStaticProfileUpdatesProfileAndRaisesCredentialProfileSaved()
        {
            const string expectedName = "TestProfileName";
            const string expectedAccessKey = _accessKey; 
            const string expectedSecretKey = _secretKey;
            const string expectedRegion = "region1-aws";

            _sut.ProfileProperties.Name = expectedName;
            _sut.ProfileProperties.AccessKey = expectedAccessKey;
            _sut.ProfileProperties.SecretKey = expectedSecretKey;
            _sut.ProfileProperties.Region = expectedRegion;

            _sut.SelectedCredentialFileType = CredentialProfileFormViewModel.CredentialFileType.Shared;

            await Assert.RaisesAsync<CredentialProfileFormViewModel.CredentialProfileSavedEventArgs>(
                handler => _sut.CredentialProfileSaved += handler,
                handler => _sut.CredentialProfileSaved -= handler,
                async () => await ((AsyncRelayCommand) _sut.SaveCommand).ExecuteAsync(null));

            _toolkitContextFixture.CredentialSettingsManager.Verify(mock => mock.CreateProfileAsync(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
                It.Is<ProfileProperties>(props =>
                    props.Name == expectedName &&
                    props.AccessKey == expectedAccessKey &&
                    props.SecretKey == expectedSecretKey &&
                    props.Region == expectedRegion),
                It.IsAny<CancellationToken>()));
        }

        // TODO IDE-10947
        //[Fact]
        //public void SaveValidSsoProfileUpdatesProfileAndRaisesCredentialProfileSaved()
        //{
        //    // Use CredentialFileType.SDK here for more coverage
        //}

        [Fact]
        public void ImportCsvLoadsCredentialsFromFileIntoFields()
        {
            using (var tempLocation = new TemporaryTestLocation())
            {
                var filePath = Path.Combine(tempLocation.OutputFolder, Guid.NewGuid().ToString());
                File.WriteAllLines(filePath, new[]
                {
                    "Access key ID,Secret access key",
                    $"{_accessKey},{_secretKey}"
                });

                var mockDialog = new Mock<IOpenFileDialog>();
                mockDialog.SetupGet(mock => mock.FileName).Returns(filePath);
                mockDialog.Setup(dialog => dialog.ShowDialog()).Returns(true);

                _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateOpenFileDialog()).Returns(mockDialog.Object);

                _sut.ImportCsvFileCommand.Execute(null);

                Assert.Equal(_accessKey, _sut.ProfileProperties.AccessKey);
                Assert.Equal(_secretKey, _sut.ProfileProperties.SecretKey);
            }
        }

        [Fact]
        public void ImportCsvDoesNothingWhenNoFilenameProvided()
        {
            var mockDialog = new Mock<IOpenFileDialog>();
            mockDialog.Setup(dialog => dialog.ShowDialog()).Returns(false);

            _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateOpenFileDialog()).Returns(mockDialog.Object);

            var propertyChangedCalled = false;
            PropertyChangedEventHandler propertyChangedHandler = (sender, e) => propertyChangedCalled = true;

            Assert.Null(_sut.ProfileProperties.AccessKey);
            Assert.Null(_sut.ProfileProperties.SecretKey);

            _sut.PropertyChanged += propertyChangedHandler;
            _sut.ImportCsvFileCommand.Execute(null);
            _sut.PropertyChanged -= propertyChangedHandler;

            Assert.False(propertyChangedCalled);
            Assert.Null(_sut.ProfileProperties.AccessKey);
            Assert.Null(_sut.ProfileProperties.SecretKey);
        }

        [Fact]
        public void ImportCsvFailsOnInvalidFileFormat()
        {
            using (var tempLocation = new TemporaryTestLocation())
            {
                var filePath = Path.Combine(tempLocation.OutputFolder, Guid.NewGuid().ToString());
                File.WriteAllLines(filePath, new[]
                {
                    "Access key ID,That ain't right",
                    $"{_accessKey},{_secretKey}"
                });

                var mockDialog = new Mock<IOpenFileDialog>();
                mockDialog.SetupGet(mock => mock.FileName).Returns(filePath);
                mockDialog.Setup(dialog => dialog.ShowDialog()).Returns(true);

                _toolkitContextFixture.DialogFactory.Setup(mock => mock.CreateOpenFileDialog()).Returns(mockDialog.Object);

                _sut.ImportCsvFileCommand.Execute(null);

                _toolkitContextFixture.ToolkitHost.Verify(mock => mock.ShowError(It.IsAny<string>()));
            }
        }

        // TODO IDE-10795 Add field validation tests

    }
}
