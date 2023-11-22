using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.VisualStudio.GettingStarted;
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;

using Moq;

using Xunit;

namespace AWSToolkitPackage.Tests.GettingStarted
{
    public class GettingStartedViewModelTests
    {
        private readonly GettingStartedViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly Mock<IAddEditProfileWizard> _addEditProfileWizardMock = new Mock<IAddEditProfileWizard>();

        private readonly Mock<IGettingStartedCompleted> _gettingStartedCompleted = new Mock<IGettingStartedCompleted>();

        private readonly List<ICredentialIdentifier> _credentialIdentifiers = new List<ICredentialIdentifier>();

        public GettingStartedViewModelTests()
        {
            _credentialIdentifiers.Add(new SharedCredentialIdentifier("default"));

            _toolkitContextFixture.CredentialManager.Setup(mock => mock.GetCredentialIdentifiers())
                .Returns(() => _credentialIdentifiers);

            _toolkitContextFixture.CredentialSettingsManager.Setup(mock => mock.GetProfileProperties(It.IsAny<ICredentialIdentifier>()))
                .Returns(() => new ProfileProperties());

            _sut = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext);
            _sut.ServiceProvider.SetService(_addEditProfileWizardMock.Object);
            _sut.ServiceProvider.SetService(_gettingStartedCompleted.Object);
        }

        private async Task RunViewModelLifecycle()
        {
            await _sut.RegisterServicesAsync();
            await _sut.InitializeAsync();
        }

        private void SetupChangeConnectionSettingsAsync(ConnectionState state)
        {
            _toolkitContextFixture.ConnectionManager.Setup(mock => mock.ChangeConnectionSettingsAsync(
                It.IsAny<ICredentialIdentifier>(), It.IsAny<ToolkitRegion>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(state);
        }

        [Fact]
        public async Task ConnectionStateIsValidSetsStatusTrue()
        {
            var id = new FakeCredentialIdentifier() { DisplayName = "nobody" };
            var region = new ToolkitRegion() { DisplayName = "nowhere-east-6" };
            SetupChangeConnectionSettingsAsync(new ConnectionState.ValidConnection(id, region));

            _gettingStartedCompleted.SetupSet(mock => mock.Status = true).Verifiable();

            await RunViewModelLifecycle();

            _sut.ShowCompleted(id);

            _gettingStartedCompleted.VerifyAll();
        }

        [Fact]
        public async Task AlwaysStartsOnAddEditProfileWizards()
        {
            await RunViewModelLifecycle();

            Assert.Equal(GettingStartedStep.AddEditProfileWizards, _sut.CurrentStep);
        }
    }
}
