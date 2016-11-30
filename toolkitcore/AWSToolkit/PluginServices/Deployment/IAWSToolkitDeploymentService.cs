using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;

using log4net;

namespace Amazon.AWSToolkit.PluginServices.Deployment
{
    public abstract class DeploymentServiceIdentifiers
    {
        public const string CloudFormationServiceName = "CloudFormation";
        public const string BeanstalkServiceName = "ElasticBeanstalk";
        public const string LambdaServiceName = "Lambda";
    }

    public abstract class DeploymentTypeIdentifiers
    {
        /// <summary>
        /// Deployment of a web app or web site project to the target service
        /// </summary>
        public const string VSToolkitDeployment = "VSToolkitDeployment";

        /// <summary>
        /// Deployment of a custom CloudFormation template 
        /// </summary>
        public const string CFNTemplateDeployment = "CFNTemplateDeployment";
    }

    /// <summary>
    /// Defines a generic interface for application deployment to an AWS service that plugins can 
    /// implement to support deployment from a toolkit shell.
    /// </summary>
    public interface IAWSToolkitDeploymentService
    {
        /// <summary>
        /// Returns the name of the underlying deployment service (from DeploymentServiceIdentifiers)
        /// </summary>
        string DeploymentServiceIdentifier { get; }

        /// <summary>
        /// Return pages relevant to (re)deployment with the service represented by the plugin.
        /// </summary>
        /// <param name="hostWizard"></param>
        /// <param name="fastTrackRedeployment">
        /// True if the plugin should contribute the minimum pages necessary to
        /// redeploy to the service, with a fixed account, region and environment.
        /// False to assume a 'full' new deployment (or redeployment).
        /// </param>
        IEnumerable<IAWSWizardPageController> ConstructDeploymentPages(IAWSWizard hostWizard, bool fastTrackRedeployment);

        /// <summary>
        /// Perform a deployment of the specified package
        /// </summary>
        /// <param name="deploymentPackage"></param>
        /// <param name="deploymentProperties"></param>
        /// <returns>True if deployment succeeded</returns>
        bool Deploy(string deploymentPackage, IDictionary<string, object> deploymentProperties);

        /// <summary>
        /// Test to see if the specified deployed environment is still available for the user. The supplied dictionary
        /// contains provider-specific data describing the environment.
        /// </summary>
        /// <param name="environmentDetails"></param>
        /// <returns></returns>
        bool IsRedeploymentTargetValid(AccountViewModel account, string region, IDictionary<string, object> environmentDetails);

        /// <summary>
        /// Returns the set of AWS Toolkit deployments in existence for this service
        /// </summary>
        /// <param name="account"></param>
        /// <param name="region"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        IEnumerable<ExistingServiceDeployment> QueryToolkitDeployments(AccountViewModel account, string region, ILog logger);
    }

    /// <summary>
    /// Used to pass deployments to different services back as a unified whole
    /// </summary>
    public class ExistingServiceDeployment
    {
        /// <summary>
        /// From DeploymentTemplateWrapperBase, the name of the service
        /// </summary>
        public string DeploymentService { get; set; }

        /// <summary>
        /// The logical name of the deployment; for CloudFormation this will be the stack
        /// name. For Beanstalk it is the application name.
        /// </summary>
        public string DeploymentName { get; set; }

        /// <summary>
        /// Additional service-dependent data of use; currently only set for CloudFormation
        /// deployments and is the model Stack instance mapping to DeploymentName
        /// </summary>
        public object Tag { get; set; }
    }
}
