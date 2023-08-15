using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Tests.Common.Context;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoConfigurationDetailsViewModelTests : IAsyncLifetime
    {
        private SsoConfigurationDetailsViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly ICredentialIdentifier _sampleCredentialIdentifier =
            new SharedCredentialIdentifier("sample-profile");

        private readonly ServiceProvider _serviceProvider = new ServiceProvider();

        public async Task InitializeAsync()
        {
            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers()).Returns(new List<ICredentialIdentifier>());

            _serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);

            _sut = await ViewModelTests.BootstrapViewModel<SsoConfigurationDetailsViewModel>(_serviceProvider);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public void ISsoProfilePropertiesProviderExposesProfileProperties()
        {
            const string expectedProfileName = "my-profile-name";
            const string expectedSsoStartUrl = "https://d-1234567890.awsapps.com/start";

            var svc = _serviceProvider.RequireService<ISsoProfilePropertiesProvider>();
            Assert.NotNull(svc);
            Assert.NotNull(svc.ProfileProperties);

            // Writes on view model are readable from service
            _sut.ProfileName = expectedProfileName;
            Assert.Equal(expectedProfileName, svc.ProfileProperties.Name);

            // Writes on service are readable from view model
            svc.ProfileProperties.SsoStartUrl = expectedSsoStartUrl;
            Assert.Equal(expectedSsoStartUrl, _sut.SsoStartUrl);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("", "startUrl")]
        [InlineData("profile", "")]
        public void ConnectCommandDisabled_WhenSsoPropertiesEmpty(string profile, string startUrl)
        {
            _sut.SsoStartUrl = startUrl;
            _sut.ProfileName = profile;

            var result = _sut.ConnectToIamIdentityCenterCommand.CanExecute(null);
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
            _sut.ProfileName = "sampleProfile";
            _sut.SsoStartUrl = startUrl;
            Assert.Single(((INotifyDataErrorInfo) _sut).GetErrors(nameof(_sut.SsoStartUrl)).OfType<object>());
        }
    }
}
