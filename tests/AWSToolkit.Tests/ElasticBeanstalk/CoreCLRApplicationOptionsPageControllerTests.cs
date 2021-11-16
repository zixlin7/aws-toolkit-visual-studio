using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

using AWSToolkit.Tests.ElasticBeanstalk.Wizard;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class CoreCLRApplicationOptionsPageControllerTests : BeanstalkWizardTests
    {
        public CoreCLRApplicationOptionsPageControllerTests()
        {
            DefaultWizard();
        }

        private void DefaultWizard()
        {
            Wizard[DeploymentWizardProperties.SeedData.propkey_ProjectBuildConfigurations] = new Dictionary<string, string>();
        }

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

        [StaTheory]
        [InlineData("net6.0", true)]
        [InlineData("net5", false)]
        [InlineData("netframework4.7", false)]
        [InlineData("netcore3.1", false)]
        [InlineData("netcore2.1", false)]
        public void ShouldDefaultSelfContained(string runtime, bool expectedDefault)
        {
            // arrange.
            SetRuntimeTo(runtime);

            var optionsPageController = new CoreCLRApplicationOptionsPageController()
            {
                HostingWizard = Wizard
            };

            // act.
            var page = InitializePageFrom(optionsPageController);

            // assert.
            Assert.Equal(runtime, page.TargetFramework);
            Assert.Equal(expectedDefault, page.BuildSelfContainedBundle);
        }

        private void SetRuntimeTo(string runtime)
        {
            Wizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = runtime;
            Wizard[DeploymentWizardProperties.SeedData.propkey_ProjectFrameworks] =
                new Dictionary<string, string>() { { runtime, runtime } };
        }

        private CoreCLRApplicationOptionsPage InitializePageFrom(CoreCLRApplicationOptionsPageController controller)
        {
            return controller.PageActivating(AWSWizardConstants.NavigationReason.movingForward) as CoreCLRApplicationOptionsPage;
        }
    }
}
