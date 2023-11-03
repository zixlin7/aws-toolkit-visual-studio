using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Tests.Common.Context;

using AWSToolkit.Tests.Credentials.Core;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    /// <summary>
    /// Unit tests for AddEditProfileWizardViewModel.
    /// </summary>
    /// <remarks>
    /// Subform loading by SelectedCredentialType is handled entirely within XAML and not tested here.
    /// </remarks>
    public class AddEditProfileWizardViewModelTests
    {
        private readonly AddEditProfileWizardViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly ServiceProvider _serviceProvider;

        private readonly Mock<IAddEditProfileWizardHost> _addEditProfileWizardHostMock = new Mock<IAddEditProfileWizardHost>();

        public AddEditProfileWizardViewModelTests()
        {
            _toolkitContextFixture.ConnectionManager.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());

            _serviceProvider = new ServiceProvider();
            _serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);

            // Doesn't matter, not enabling telemetry anyway
            _addEditProfileWizardHostMock.Setup(mock => mock.SaveMetricSource).Returns(CommonMetricSources.AwsExplorerMetricSource.ServiceNode);
            _serviceProvider.SetService(_addEditProfileWizardHostMock.Object);

            _sut = new AddEditProfileWizardViewModel
            {
                ServiceProvider = _serviceProvider
            };
        }

        [Fact]
        public async Task SaveValidStaticProfileUpdatesProfileAndRaisesConnectionSettingsChanged()
        {
            const string expectedName = "TestStaticProfileName";
            const string expectedAccessKey = "ACCESSKEY4THETESTYAY"; // 20 chars starting with an "A" is typical
            const string expectedSecretKey = "aaaaaabbbbbbbbccccccccddddddddeeeeeeeffffffgggggghhhhh";
            const string expectedRegion = ToolkitRegion.DefaultRegionId;

            var p = new ProfileProperties()
            {
                Name = expectedName,
                AccessKey = expectedAccessKey,
                SecretKey = expectedSecretKey,
                Region = expectedRegion
            };

            await _sut.SaveAsync(p, CredentialFileType.Shared);

            _addEditProfileWizardHostMock.Verify(mock => mock.NotifyConnectionSettingsChanged(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName)));

            _toolkitContextFixture.CredentialSettingsManager.Verify(mock => mock.CreateProfileAsync(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
                It.Is<ProfileProperties>(props =>
                    props.Name == expectedName &&
                    props.AccessKey == expectedAccessKey &&
                    props.SecretKey == expectedSecretKey &&
                    props.Region == expectedRegion),
                It.IsAny<CancellationToken>()));

            _toolkitContextFixture.ConnectionManager.Verify(mock => mock.ChangeConnectionSettingsAsync(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
                It.Is<ToolkitRegion>(region => region.Id == expectedRegion),
                It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task SaveValidSsoProfileUpdatesProfileAndRaisesConnectionSettingsChanged()
        {
            const string expectedName = "TestSsoProfileName";
            const string expectedRegion = ToolkitRegion.DefaultRegionId;
            const string expectedSsoAccountId = "123456789012";
            const string expectedSsoRoleName = "myRole";
            const string expectedSsoSession = "mySession";
            const string expectedSsoStartUrl = "https://amazon.com/roadtonowhere";

            var p = new ProfileProperties()
            {
                Name = expectedName,
                Region = expectedRegion,
                SsoAccountId = expectedSsoAccountId,
                SsoRegion = expectedRegion,
                SsoRoleName = expectedSsoRoleName,
                SsoSession = expectedSsoSession,
                SsoStartUrl = expectedSsoStartUrl
            };

            await _sut.SaveAsync(p, CredentialFileType.Shared);

            _addEditProfileWizardHostMock.Verify(mock => mock.NotifyConnectionSettingsChanged(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName)));

            _toolkitContextFixture.CredentialSettingsManager.Verify(mock => mock.CreateProfileAsync(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
                It.Is<ProfileProperties>(props =>
                    props.Name == expectedName &&
                    props.Region == expectedRegion &&
                    props.SsoAccountId == expectedSsoAccountId &&
                    props.SsoRegion == expectedRegion &&
                    props.SsoRoleName == expectedSsoRoleName &&
                    props.SsoSession == expectedSsoSession &&
                    props.SsoStartUrl == expectedSsoStartUrl),
                It.IsAny<CancellationToken>()));

            _toolkitContextFixture.ConnectionManager.Verify(mock => mock.ChangeConnectionSettingsAsync(
                It.Is<SharedCredentialIdentifier>(id => id.ProfileName == expectedName),
                It.Is<ToolkitRegion>(region => region.Id == expectedRegion),
                It.IsAny<CancellationToken>()));
        }
    }
}
