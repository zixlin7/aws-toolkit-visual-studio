using System;

using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    internal class Project
    {
        /// <summary>
        /// Indicates if the current configuration would deploy a .NET Core project
        /// </summary>
        internal static bool IsNetCoreWebProject(IAWSWizard wizard)
        {
            var projectType = wizard.GetProperty(DeploymentWizardProperties.SeedData.propkey_ProjectType) as string;
            return projectType != null && projectType.Equals(DeploymentWizardProperties.NetCoreWebProject,
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Indicates if the current configuration would deploy a standard asp.net project
        /// (.NET Framework)
        /// </summary>
        /// <param name="wizard"></param>
        internal static bool IsStandardWebProject(IAWSWizard wizard)
        {
            var projectType = wizard.GetProperty(DeploymentWizardProperties.SeedData.propkey_ProjectType) as string;
            return projectType == null ||
                   !projectType.Equals(DeploymentWizardProperties.NetCoreWebProject,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
