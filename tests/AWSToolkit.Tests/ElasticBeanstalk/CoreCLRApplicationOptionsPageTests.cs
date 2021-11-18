using System.Windows;

using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class CoreCLRApplicationOptionsPageTests : BeanstalkWizardTests
    {
        [StaFact]
        public void ShouldHaveSelfContainedVisible()
        {
            SetProjectTypeTo(DeploymentWizardProperties.NetCoreWebProject);
            ShouldHaveSelfContainedVisibility(Visibility.Visible);
        }

        private void ShouldHaveSelfContainedVisibility(Visibility expectedVisibility)
        {
            var page = CreatePage();
            Assert.Equal(expectedVisibility, page.SelfContainedVisibility);
        }

        [StaFact]
        public void ShouldHaveSelfContainedCollapsed()
        {
            SetProjectTypeTo(DeploymentWizardProperties.StandardWebProject);
            ShouldHaveSelfContainedVisibility(Visibility.Collapsed);
        }

        private CoreCLRApplicationOptionsPage CreatePage()
        {
            return new CoreCLRApplicationOptionsPage() { PageController = CreateController() };
        }

        private CoreCLRApplicationOptionsPageController CreateController()
        {
            return new CoreCLRApplicationOptionsPageController() { HostingWizard = Wizard };
        }
    }
}
