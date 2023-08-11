﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

        private readonly ICredentialIdentifier _sampleCredentialIdentifier =
            new SharedCredentialIdentifier("sample-profile");

        public CredentialProfileFormViewModelTests()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());

            _toolkitContextFixture.ToolkitContext.ConnectionManager = connectionManagerMock.Object;

            _sut = new CredentialProfileFormViewModel(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public async Task SaveValidStaticProfileUpdatesProfile()
        {
            const string expectedName = "TestProfileName";
            const string expectedAccessKey = _accessKey; 
            const string expectedSecretKey = _secretKey;
            const string expectedRegion = "region1-aws";

            _sut.ProfileName = expectedName;
            _sut.AccessKey = expectedAccessKey;
            _sut.SecretKey = expectedSecretKey;
            _sut.ProfileProperties.Region = expectedRegion;

            // Temporarily unset ProfileProperties.SsoRegion so CreateCredentialProfile doesn't think this is
            // an SSO profile.  This is known issue and is fixed along with this test in an upcoming PR.
            _sut.ProfileProperties.SsoRegion = null;

            _sut.SelectedCredentialFileType = CredentialProfileFormViewModel.CredentialFileType.Shared;

            await ((AsyncRelayCommand) _sut.SaveCommand).ExecuteAsync(null);

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
            _sut.SelectedCredentialType = CredentialType.StaticProfile;
            _sut.AccessKey = access;
            _sut.SecretKey = secret;
            _sut.ProfileName = profile;

            var result = _sut.SaveCommand.CanExecute(null);
            Assert.False(result);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("", "startUrl")]
        [InlineData("profile", "")]
        public void SaveCommandDisabled_WhenSsoPropertiesEmpty(string profile, string startUrl)
        {
            _sut.SelectedCredentialType = CredentialType.SsoProfile;
            _sut.SsoStartUrl = startUrl;
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
            _sut.AccessKey = accessKey;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.AccessKey)).OfType<object>());
        }

        [Theory]
        [InlineData("")]
        [InlineData("http://abc.com/start")]
        [InlineData("https://abc.com/end")]
        [InlineData("abc://xyz.com/")]
        [InlineData("https://xyz.com/")]
        [InlineData("https://xyz.apps.com/start")]
        [InlineData("https://awsapps.com/start")]
        [InlineData("hello")]
        public void SsoStartUrlError_WhenInvalidValue(string startUrl)
        {
            SetupValidRequiredProperties();
            _sut.SsoStartUrl = startUrl;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.SsoStartUrl)).OfType<object>());
        }

        private void SetupValidRequiredProperties()
        {
            _sut.AccessKey = "sampleAccess";
            _sut.SecretKey = "sampleSecret";
            _sut.ProfileName = "sampleProfile";
            _sut.SsoStartUrl = "sampleStartUrl";
        }
    }
}
