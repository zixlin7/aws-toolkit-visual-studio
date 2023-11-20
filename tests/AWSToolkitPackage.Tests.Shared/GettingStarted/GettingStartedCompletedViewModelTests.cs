using System.Threading.Tasks;
using Amazon.AWSToolkit.VisualStudio.GettingStarted;
using Amazon.AWSToolkit.VisualStudio.GettingStarted.Services;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Moq;
using FluentAssertions;
using Xunit;

namespace AWSToolkitPackage.Tests.GettingStarted
{
    public class GettingStartedCompletedViewModelTests
    {
        private readonly GettingStartedCompletedViewModel _sut;

        private readonly IGettingStarted _gettingStarted;

        private readonly Mock<IAddEditProfileWizard> _addEditProfileWizardMock = new Mock<IAddEditProfileWizard>();

        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();

        public GettingStartedCompletedViewModelTests()
        {
            _sut = new GettingStartedCompletedViewModel();

            _gettingStarted = new GettingStartedViewModel(_toolkitContextFixture.ToolkitContext);

            CreateServiceProvider();

            _sut.ServiceProvider.SetService(_addEditProfileWizardMock.Object);

            _sut.ServiceProvider.SetService(_gettingStarted);
        }

        private void CreateServiceProvider()
        {
            var serviceProvider = new ServiceProvider();

            serviceProvider.SetService(_toolkitContextFixture.ToolkitContext);

            _sut.ServiceProvider = serviceProvider;
        }

        [Fact]
        public async Task ShouldOpenAddEditWizard()
        {
            await RunViewModelLifecycle();

            _gettingStarted.CurrentStep = GettingStartedStep.GettingStartedCompleted;
                
            _sut.OpenAddEditWizardCommand.Execute(this);

            _gettingStarted.CurrentStep.Should().Be(GettingStartedStep.AddEditProfileWizards);

            _gettingStarted.FeatureType.Should()
                .Be(_sut.IsCodeWhispererSupported ? FeatureType.CodeWhisperer : FeatureType.AwsExplorer);
        }

        private async Task RunViewModelLifecycle()
        {
            await _sut.RegisterServicesAsync();

            await _sut.InitializeAsync();
        }
    }
}
