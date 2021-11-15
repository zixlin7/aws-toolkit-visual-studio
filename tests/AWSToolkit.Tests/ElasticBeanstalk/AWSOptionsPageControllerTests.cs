using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

using AWSToolkit.Tests.ElasticBeanstalk.Wizard;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class AWSOptionsPageControllerTests : BeanstalkWizardControllerTests
    {
        public class TestableAWSOptionsPageController : AWSOptionsPageController
        {
            public TestableAWSOptionsPageController(AWSOptionsPage optionsPage)
            {
                _pageUI = optionsPage;
            }
        }

        private readonly AWSOptionsPageController _optionsPageController;

        public AWSOptionsPageControllerTests()
        {
            _optionsPageController = CreateController(new AWSOptionsPage());
        }

        private AWSOptionsPageController CreateController(AWSOptionsPage optionsPage)
        {
            return new TestableAWSOptionsPageController(optionsPage) { HostingWizard = Wizard };
        }

        [StaFact]
        public void ShouldNotSetEbTools()
        {
            // arrange.
            SetProjectTypeTo(DeploymentWizardProperties.NetCoreWebProject);

            // act.
            // using movingBack to avoid a block of hard to test code.
            TransferStateFromPageToWizard(_optionsPageController, AWSWizardConstants.NavigationReason.movingBack);

            // assert.
            Wizard.AssertDoesNotContainEbToolsProperty();
        }

    }
}
