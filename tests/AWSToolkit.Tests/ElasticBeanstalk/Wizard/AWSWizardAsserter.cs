﻿using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
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

        public static void AssertSelfContained(this IAWSWizard wizard, bool isSelfContained)
        {
            Assert.Equal(isSelfContained, wizard[DeploymentWizardProperties.AppOptions.propkey_BuildSelfContainedBundle]);
        }


        public static void AssertUsingEbTools(this IAWSWizard wizard)
        {
            Assert.True(UsingEbTools(wizard));
        }

        private static bool UsingEbTools(IAWSWizard wizard)
        {
            return true.Equals(wizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_UseEbToolsToDeploy]);
        }

        public static void AssertNotUsingEbTools(this IAWSWizard wizard)
        {
            Assert.False(UsingEbTools(wizard));
        }

        public static void AssertDoesNotContainEbToolsProperty(this IAWSWizard wizard)
        {
            Assert.False(wizard.IsPropertySet(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_UseEbToolsToDeploy));
        }
    }
}
