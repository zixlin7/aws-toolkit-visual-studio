using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;

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
        private readonly CredentialProfileFormViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public CredentialProfileFormViewModelTests()
        {
            _sut = new CredentialProfileFormViewModel(_toolkitContextFixture.ToolkitContext);
        }

        [Fact]
        public void SaveValidStaticProfileUpdatesProfileAndRaisesCredentialProfileSaved()
        {
            const string expectedName = "TestProfileName";
            const string expectedAccessKey = "ACCESSKEY4THETESTYAY"; // 20 chars starting with an "A" is typical
            const string expectedSecretKey = "alkfjhsfksjhfalsfhsafljweirwriywiruyweorwelkfjasfdjk";
            const string expectedRegion = "us-best-2";

            _sut.ProfileProperties.Name = expectedName;
            _sut.ProfileProperties.AccessKey = expectedAccessKey;
            _sut.ProfileProperties.SecretKey = expectedSecretKey;
            _sut.ProfileProperties.Region = expectedRegion;

            _sut.SelectedCredentialFileType = CredentialProfileFormViewModel.CredentialFileType.Shared;

            Assert.Raises<CredentialProfileFormViewModel.CredentialProfileSavedEventArgs>(
                handler => _sut.CredentialProfileSaved += handler,
                handler => _sut.CredentialProfileSaved -= handler,
                () => _sut.SaveCommand.Execute(null));            

            _toolkitContextFixture.CredentialSettingsManager.Verify(mock => mock.CreateProfile(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
                It.Is<ProfileProperties>(props =>
                    props.Name == expectedName &&
                    props.AccessKey == expectedAccessKey &&
                    props.SecretKey == expectedSecretKey &&
                    props.Region == expectedRegion)));
        }

        // TODO IDE-10947
        //[Fact]
        //public void SaveValidSsoProfileUpdatesProfileAndRaisesCredentialProfileSaved()
        //{
        //    // Use CredentialFileType.SDK here for more coverage
        //}

        // TODO IDE-10794
        //[Fact]
        //public void ImportCsvLoadsCredentialsFromFileIntoFields()
        //{

        //}

        // TODO IDE-10912
        //[Fact]
        //public void OpenCredentialFileAttemptsToOpenNotepadFileCorrectPath()
        //{

        //}

        // TODO IDE-10795 Add field validation tests

    }
}
