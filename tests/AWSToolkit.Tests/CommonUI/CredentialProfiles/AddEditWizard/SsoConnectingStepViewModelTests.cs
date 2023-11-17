using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;

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

        private readonly ProfileProperties _profileProperties = new ProfileProperties();

        public async Task InitializeAsync()
        {
            _profileProperties.Name = "my-profile";
            _profileProperties.SsoRegion = RegionEndpoint.USWest2.SystemName;
            // Bad URLs will be accepted by AWSSDK if they follow the https://<...>.awsapps.com/start pattern
            _profileProperties.SsoStartUrl = "https://roadtonowhere.awsapps.com/start";

            _mockPropertiesProvider.SetupGet(mock => mock.ProfileProperties).Returns(_profileProperties);

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
            _profileProperties.SsoStartUrl = "https://amazon.com/roadtonowhere";

            // Call view model lifecycle method ViewShownAsync to call ResolveAwsTokenAsync indirectly.
            // This should fail almost immediately as expected due to bogus SsoStartUrl, but set a reasonable 
            // timeout less than the 10 minute grant code timeout just in case an update breaks the test.
            await ConstrainTaskLengthAsync(_sut.ViewShownAsync(), TimeSpan.FromMinutes(1));

            _toolkitContextFixture.ToolkitHost.Verify(th => th.ShowError(It.IsAny<string>()), Times.Once());
        }

        /// <summary>
        /// Throws if the task runs too long
        /// </summary>
        private async Task ConstrainTaskLengthAsync(Task task, TimeSpan taskLimit)
        {
            var tokenSource = new CancellationTokenSource();
            tokenSource.CancelAfter(taskLimit);

            await Task.WhenAny(task, Task.Delay(Timeout.Infinite, tokenSource.Token));
        }

        [Fact]
        public async Task ResolveAwsTokenAsyncIsCancelleableOnFirstCall()
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

        [Fact]
        public async Task ResolveAwsTokenAsyncIsCancelledThenCalledAgainIsCancellable()
        {
            var cancelToken = new CancellationTokenSource();
            var vm = _serviceProvider.RequireService<IResolveAwsToken>() as SsoConnectingStepViewModel;
            Assert.True(vm != null, $"Requires access to {nameof(SsoConnectingStepViewModel)} for this test.");

            // First cancel
            // Cancel before calling to avoid race conditions
            cancelToken.Cancel();
            await vm.ResolveAwsTokenAsync(cancelToken);

            Assert.Equal(WizardStep.Configuration, _mockWizard.Object.CurrentStep);

            // Second cancel
            // ResolveAwsTokenAsync checks every second, so delay a few seconds to ensure AWSSDK call has started
            var task = vm.ResolveAwsTokenAsync(cancelToken);
            await Task.Delay(TimeSpan.FromSeconds(3));
            var actual = await task;

            Assert.Null(actual);

            // Since the AWSSDK polling process isn't cancellable, just verify that we've reverted to
            // the first step of the wizard as it will take 10 minutes for the polling to terminate
            // due to the code expiring.
            Assert.Equal(WizardStep.Configuration, _mockWizard.Object.CurrentStep);

            _toolkitContextFixture.ToolkitHost.Verify(th => th.ShowError(It.IsAny<string>()), Times.Never());
        }
    }
}
