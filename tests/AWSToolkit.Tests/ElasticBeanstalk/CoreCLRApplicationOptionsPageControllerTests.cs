using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

using AWSToolkit.Tests.ElasticBeanstalk.Wizard;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class CoreCLRApplicationOptionsPageControllerTests : BeanstalkWizardControllerTests
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

            // act.
            var optionsPageController = new TestableCoreCLRApplicationOptionsPageController(page)
            {
                HostingWizard = Wizard
            };

            TransferStateFromPageToWizard(optionsPageController);

            // assert.
            Wizard.AssertPlatformIsWindows();
            Wizard.AssertIsSelfContained();
        }
    }
}
