using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Publish.Banner;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Settings.Publish;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Publish.Banner
{
    public class PublishBannerPropertyChangedHandlerTests
    {
        private readonly PublishBannerViewModel _publishBanner;
        private readonly Mock<IAWSWizard> _spyAwsWizard;

        private readonly PublishBannerPropertyChangedHandler _propertyChangedHandler;

        public PublishBannerPropertyChangedHandlerTests()
        {
            _publishBanner = CreateViewModel();
            _spyAwsWizard = new Mock<IAWSWizard>();
            _propertyChangedHandler = new PublishBannerPropertyChangedHandler(_publishBanner, _spyAwsWizard.Object);
        }

        private PublishBannerViewModel CreateViewModel()
        {
            return new PublishBannerViewModel(CreateToolkitContext(), new InMemoryPublishSettingsRepository());
        }

        private ToolkitContext CreateToolkitContext()
        {
            return new ToolkitContext() { ToolkitHost = new ProjectToolkitShellProvider(ProjectFixture.Create()) };
        }

        [Fact]
        public void ShouldCloseWizardWhenClosedChangedToTrue()
        {
            // act.
            _publishBanner.CloseCurrentPublishExperience = true;

            // assert.
            _spyAwsWizard.Verify(m => m.CancelRun(), Times.Once);
        }

        [Fact]
        public void ShouldNotCloseWizardWhenCloseChangedToFalse()
        {
            // act.
            _publishBanner.CloseCurrentPublishExperience = false;

            // assert.
            _spyAwsWizard.Verify(m => m.CancelRun(), Times.Never);
        }

        [Fact]
        public void ShouldCloseWizardWithNoChange()
        {
            // assert.
            _spyAwsWizard.Verify(m => m.CancelRun(), Times.Never);
        }
    }
}
