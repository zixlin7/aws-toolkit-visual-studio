using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;

using AWSToolkit.Tests.Credentials.Core;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    public class StaticConfigurationStepViewModelTests : IAsyncLifetime
    {
        private const string _accessKey = "ACCESSKEY4THETESTYAY"; // 20 chars starting with an "A" is typical

        private const string _secretKey = "aaaaaabbbbbbbbccccccccddddddddeeeeeeeffffffgggggghhhhh";

        private StaticConfigurationDetailsViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly ICredentialIdentifier _sampleCredentialIdentifier =
            new SharedCredentialIdentifier("sample-profile");

        private readonly ServiceProvider _serviceProvider = new ServiceProvider();

        public async Task InitializeAsync()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            _toolkitContextFixture.ToolkitContext.ConnectionManager = connectionManagerMock.Object;
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());

            _serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);

            _sut = await ViewModelTests.BootstrapViewModel<StaticConfigurationDetailsViewModel>(_serviceProvider);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

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

        [Theory]
        [InlineData("", "", "")]
        [InlineData("", "access", "secret")]
        [InlineData("profile", "", "secret")]
        [InlineData("", "", "secret")]
        [InlineData("profile", "", "")]
        [InlineData("", "access", "")]
        [InlineData("profile", "access", "")]
        public void SaveCommandDisabled_WhenStaticPropertiesEmpty(string profile, string access, string secret)
        {
            _sut.AccessKeyID = access;
            _sut.SecretKey = secret;
            _sut.ProfileName = profile;

            var result = _sut.SaveCommand.CanExecute(null);
            Assert.False(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("@1244AD")]
        [InlineData("abchf-6;")]
        public void ProfileNameError_WhenInvalidName(string profileName)
        {
            _sut.ProfileName = profileName;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.ProfileName)).OfType<object>());
        }


        [Fact]
        public void ProfileNameError_WhenAlreadyExistingName()
        {
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers())
                .Returns(new List<ICredentialIdentifier>() { _sampleCredentialIdentifier });
            _sut.ProfileName = _sampleCredentialIdentifier.ProfileName;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.ProfileName)).OfType<object>());
        }

        [Fact]
        public void SecretKeyError_WhenInvalid()
        {
            SetupValidRequiredProperties();
            _sut.SecretKey = string.Empty;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.SecretKey)).OfType<object>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("1244AD")]
        [InlineData("abchaaaaaaaaaaaayyyyf-6")]
        [InlineData("abchaaaaaaaaaa12344@@")]
        public void AccessKeyError_WhenInvalidValue(string accessKey)
        {
            SetupValidRequiredProperties();
            _sut.AccessKeyID = accessKey;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.AccessKeyID)).OfType<object>());
        }

        private void SetupValidRequiredProperties()
        {
            _sut.AccessKeyID = "sampleAccess";
            _sut.SecretKey = "sampleSecret";
            _sut.ProfileName = "sampleProfile";
        }
    }
}
