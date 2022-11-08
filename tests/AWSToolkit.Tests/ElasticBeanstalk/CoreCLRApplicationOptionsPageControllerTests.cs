using System;
using System.Collections.Generic;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk;
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
        [InlineData(Frameworks.Net70, "Linux", true)]
        [InlineData(Frameworks.Net70, "Windows", true)]
        [InlineData(Frameworks.Net60, "Windows", false)]
        [InlineData(Frameworks.Net60, "Linux", false)]
        [InlineData(Frameworks.Net50, "Windows", false)]
        [InlineData(Frameworks.Net50, "Linux", false)]
        [InlineData(Frameworks.NetCoreApp31, "Windows", false)]
        [InlineData(Frameworks.NetCoreApp31, "Linux", false)]
        [InlineData(Frameworks.NetCoreApp21, "Windows", false)]
        [InlineData(Frameworks.NetCoreApp21, "Linux", false)]
        [InlineData(Frameworks.NetFramework47, "Windows", false)]
        [InlineData(Frameworks.NetFramework47, "Linux", false)]
        public void ShouldDefaultSelfContained(string runtime, string instance, bool expectedDefault)
        {
            // arrange.
            SetRuntimeTo(runtime);
            SetDeploymentTo(instance);

            var optionsPageController = new TestableCoreCLRApplicationOptionsPageController(null)
            {
                HostingWizard = Wizard
            };

            // act.
            var page = InitializePageFrom(optionsPageController);

            // assert.
            Assert.Equal(runtime, page.TargetFramework);
            Assert.Equal(expectedDefault, page.BuildSelfContainedBundle);
        }

        [StaFact]
        public void ShouldRetainDefaultAfterInstanceSwitch()
        {
            // arrange.
            SetRuntimeTo(Frameworks.Net70);
            SetDeploymentTo("Linux");

            var optionsPageController = new TestableCoreCLRApplicationOptionsPageController(null)
            {
                HostingWizard = Wizard
            };

            var page = InitializePageFrom(optionsPageController);
            Assert.True(page.BuildSelfContainedBundle);

            // act.
            SetDeploymentTo("Windows");
            // throws on wizard's unimplemented GetProperty, which occurs after default is reset
            Assert.Throws<NotImplementedException>(() => ActivatePageFrom(optionsPageController));
            TransferStateFromPageToWizard(optionsPageController);

            // assert.
            Wizard.AssertPlatformIsWindows();
            Wizard.AssertIsSelfContained();
        }

        private void SetRuntimeTo(string runtime)
        {
            Wizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = runtime;
            Wizard[DeploymentWizardProperties.SeedData.propkey_ProjectFrameworks] =
                new Dictionary<string, string>() { { runtime, runtime } };
        }

        private void SetDeploymentTo(string instance)
        {
            Wizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack] = instance.Equals("Linux");
        }

        private CoreCLRApplicationOptionsPage InitializePageFrom(CoreCLRApplicationOptionsPageController controller)
        {
            return controller.PageActivating(AWSWizardConstants.NavigationReason.movingForward) as CoreCLRApplicationOptionsPage;
        }

        private void ActivatePageFrom(CoreCLRApplicationOptionsPageController controller)
        {
            controller.PageActivated(AWSWizardConstants.NavigationReason.movingForward);
        }
    }
}
