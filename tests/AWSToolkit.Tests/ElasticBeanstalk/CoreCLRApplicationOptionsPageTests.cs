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
            // arrange.
            var controller = new CoreCLRApplicationOptionsPageController() { HostingWizard = Wizard };

            // act.
            var page = new CoreCLRApplicationOptionsPage() { PageController = controller };

            // assert.
            Assert.Equal(expectedVisibility, page.SelfContainedVisibility);
        }

        [StaFact]
        public void ShouldHaveSelfContainedCollapsed()
        {
            SetProjectTypeTo(DeploymentWizardProperties.StandardWebProject);
            ShouldHaveSelfContainedVisibility(Visibility.Collapsed);
        }
    }
}
