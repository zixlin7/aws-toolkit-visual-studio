using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Context;
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

        public AddEditProfileWizardViewModelTests()
        {
            _toolkitContextFixture.ConnectionManager.SetupGet(mock => mock.IdentityResolver).Returns(new FakeIdentityResolver());
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());

            _serviceProvider = new ServiceProvider();
            _serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);

            _sut = new AddEditProfileWizardViewModel
            {
                ServiceProvider = _serviceProvider,
                SaveMetricSource = CommonMetricSources.AwsExplorerMetricSource.ServiceNode // Doesn't matter, not enabling telemetry anyway
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

            await Assert.RaisesAsync<ConnectionSettingsChangeArgs>(
                handler => _sut.ConnectionSettingsChanged += handler,
                handler => _sut.ConnectionSettingsChanged -= handler,
                async () => await _sut.SaveAsync(p, CredentialFileType.Shared));

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

            await Assert.RaisesAsync<ConnectionSettingsChangeArgs>(
                handler => _sut.ConnectionSettingsChanged += handler,
                handler => _sut.ConnectionSettingsChanged -= handler,
                async () => await _sut.SaveAsync(p, CredentialFileType.Shared));

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
