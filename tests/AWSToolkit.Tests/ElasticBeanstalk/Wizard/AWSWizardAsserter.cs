using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk.Wizard
{
    public static class AWSWizardAsserter
    {
        public static void AssertPlatformIsLinux(this IAWSWizard wizard)
        {
            Assert.True(IsPlatformLinux(wizard));
        }

        private static bool IsPlatformLinux(IAWSWizard wizard)
        {
            return true.Equals(wizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack]);
        }

        public static void AssertPlatformIsWindows(this IAWSWizard wizard)
        {
            Assert.False(IsPlatformLinux(wizard));
        }

        public static void AssertIsSelfContained(this IAWSWizard wizard)
        {
            Assert.Equal(true, wizard[DeploymentWizardProperties.AppOptions.propkey_BuildSelfContainedBundle]);
        }
    }
}
