using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk
{
    /// <summary>
    /// General purpose interface onto the Elastic Beanstalk plugin
    /// </summary>
    public interface IAWSElasticBeanstalk
    {
        /// <summary>
        /// Returns the deployment service for Elastic Beanstalk
        /// </summary>
        IAWSToolkitDeploymentService DeploymentService { get; }
    }
}
