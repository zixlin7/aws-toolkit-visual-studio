using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.Tests.Common.Wizard;

using AWSToolkit.Tests.ElasticBeanstalk.Wizard;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class CoreCLRApplicationOptionsPageControllerTests
    {
        public class TestableCoreCLRApplicationOptionsPageController : CoreCLRApplicationOptionsPageController
        {
            public TestableCoreCLRApplicationOptionsPageController(CoreCLRApplicationOptionsPage optionsPage)
            {
                _pageUI = optionsPage;
            }
        }

        [StaFact]
        public void ShouldSetContainedOnWindows()
        {
            // arrange.
            var page = new CoreCLRApplicationOptionsPage()
            {
                IsLinuxDeployment = false,
                BuildSelfContainedBundle = true
            };

            var wizard = new InMemoryAWSWizard();

            // act.
            var optionsPageController = new TestableCoreCLRApplicationOptionsPageController(page)
            {
                HostingWizard = wizard
            };

            TransferStateFromPageToWizard(optionsPageController);

            // assert.
            wizard.AssertPlatformIsWindows();
            wizard.AssertIsSelfContained();
        }

        private void TransferStateFromPageToWizard(IAWSWizardPageController controller)
        {
            controller.PageDeactivating(AWSWizardConstants.NavigationReason.finishPressed);
        }
    }
}
