using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoConnectingStepViewModelTests : IAsyncLifetime
    {
        private SsoConnectingStepViewModel _sut;

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        private readonly ServiceProvider _serviceProvider = new ServiceProvider();

        private readonly Mock<IAddEditProfileWizard> _mockWizard = new Mock<IAddEditProfileWizard>();

        private readonly Mock<ISsoProfilePropertiesProvider> _mockPropertiesProvider = new Mock<ISsoProfilePropertiesProvider>();

        public async Task InitializeAsync()
        {
            _mockPropertiesProvider.SetupGet(mock => mock.ProfileProperties).Returns(new ProfileProperties()
            {
                Name = "my-profile",
                SsoRegion = ToolkitRegion.DefaultRegionId,
                SsoStartUrl = "https://amazon.com/roadtonowhere"
            });

            _serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);
            _serviceProvider.SetService(_mockWizard.Object);
            _serviceProvider.SetService(_mockPropertiesProvider.Object);

            _sut = await ViewModelTests.BootstrapViewModel<SsoConnectingStepViewModel>(_serviceProvider);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task ResolveAwsTokenAsyncReturnsFalseOnBadSsoStartUrl()
        {
            // Call view model lifecycle method ViewShownAsync to call ResolveAwsTokenAsync indirectly.
            // This should fail almost immediately as expected due to bogus SsoStartUrl, but set a reasonable 
            // timeout less than the 10 minute grant code timeout just in case an update breaks the test.
            await _sut.ViewShownAsync().WithTimeout(TimeSpan.FromMinutes(1));

            _toolkitContextFixture.ToolkitHost.Verify(th => th.ShowError(It.IsAny<string>()), Times.Once());
        }

        [Fact]
        public async Task ResolveAwsTokenAsyncIsCancelleable()
        {
            // Call ResolveAwsTokenAsync indirectly to ensure it is exposed via ServiceProvider
            var cancelToken = new CancellationTokenSource();
            var vm = _serviceProvider.RequireService<IResolveAwsToken>() as SsoConnectingStepViewModel;
            Assert.True(vm != null, $"Requires access to {nameof(SsoConnectingStepViewModel)} for this test.");

            // Cancel before calling to avoid race conditions
            cancelToken.Cancel();
            var actual = await vm.ResolveAwsTokenAsync(cancelToken);

            Assert.Null(actual);

            // Since the AWSSDK polling process isn't cancellable, just verify that we've reverted to
            // the first step of the wizard as it will take 10 minutes for the polling to terminate
            // due to the code expiring.
            Assert.Equal(WizardStep.Configuration, _mockWizard.Object.CurrentStep);

            _toolkitContextFixture.ToolkitHost.Verify(th => th.ShowError(It.IsAny<string>()), Times.Never());
        }
    }
}
