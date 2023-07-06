using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;

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
        private readonly CredentialProfileFormViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public CredentialProfileFormViewModelTests()
        {
            var connectionManagerMock = new Mock<IAwsConnectionManager>();
            connectionManagerMock.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());

            _toolkitContextFixture.ToolkitContext.ConnectionManager = connectionManagerMock.Object;

            _sut = new CredentialProfileFormViewModel(_toolkitContextFixture.ToolkitContext);
        }

        // TODO IDE-11099
        // Temporarily commenting out until IDE-11099 PR to re-enable as that is when SaveCommand will change
        // to AsyncRelayCommand that is now needed for this test
        //[Fact]
        //public async Task SaveValidStaticProfileUpdatesProfileAndRaisesCredentialProfileSaved()
        //{
        //    const string expectedName = "TestProfileName";
        //    const string expectedAccessKey = "ACCESSKEY4THETESTYAY"; // 20 chars starting with an "A" is typical
        //    const string expectedSecretKey = "alkfjhsfksjhfalsfhsafljweirwriywiruyweorwelkfjasfdjk";
        //    const string expectedRegion = "region1-aws";
        //    var expectedCancellationToken = new CancellationToken();

        //    _sut.ProfileProperties.Name = expectedName;
        //    _sut.ProfileProperties.AccessKey = expectedAccessKey;
        //    _sut.ProfileProperties.SecretKey = expectedSecretKey;
        //    _sut.ProfileProperties.Region = expectedRegion;

        //    _sut.SelectedCredentialFileType = CredentialProfileFormViewModel.CredentialFileType.Shared;

        //    await Assert.RaisesAsync<CredentialProfileFormViewModel.CredentialProfileSavedEventArgs>(
        //        handler => _sut.CredentialProfileSaved += handler,
        //        handler => _sut.CredentialProfileSaved -= handler,
        //        async () => await ((AsyncRelayCommand) _sut.SaveCommand).ExecuteAsync(null));            

        //    _toolkitContextFixture.CredentialSettingsManager.Verify(mock => mock.CreateProfileAsync(
        //        It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
        //        It.Is<ProfileProperties>(props =>
        //            props.Name == expectedName &&
        //            props.AccessKey == expectedAccessKey &&
        //            props.SecretKey == expectedSecretKey &&
        //            props.Region == expectedRegion),
        //        It.Is<CancellationToken>(cancellationToken => cancellationToken == expectedCancellationToken)));
        //}

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
