using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CloudFormation
{
    /// <summary>
    /// General purpose interface onto the CloudFormation plugin
    /// </summary>
    public interface IAWSCloudFormation
    {
        /// <summary>
        /// Returns the deployment service for CloudFormation
        /// </summary>
        IAWSToolkitDeploymentService DeploymentService { get; }

        /// <summary>
        /// Deploys a template, returning persistable details of the deployment or null if the deployment
        /// process did not complete successfully
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="seedParameters"></param>
        /// <returns></returns>
        DeployedTemplateData DeployCloudFormationTemplate(string filepath, IDictionary<string, object> seedParameters);
    }

    /// <summary>
    /// Carries persistable data about a template deployment or cost estimatation back to the host shell
    /// </summary>
    public class DeployedTemplateData
    {
        public enum DeploymentType { newStack, updateStack, costEstimation };

        /// <summary>
        /// What kind of deployment was done
        /// </summary>
        public DeploymentType DeploymentOperation;

        /// <summary>
        /// Filename or other reference to the template that was used
        /// </summary>
        public string TemplateUri { get; set; }

        /// <summary>
        /// The name of the stack that was created, if any
        /// </summary>
        public string StackName { get; set; }

        /// <summary>
        /// Displayable properties the user entered for the template; properties
        /// set to 'noecho' are excluded from the set
        /// </summary>
        public IDictionary<string, object> TemplateProperties { get; set; }

        /// <summary>
        /// The account used to drive the deployment/cost estimation
        /// </summary>
        public AccountViewModel Account { get; set; }

        /// <summary>
        /// The region in which the deployment or estimation was performed
        /// </summary>
        public ToolkitRegion Region { get; set; }

        /// <summary>
        /// An AWS Simple Monthly Calculator URL with a query string that describes the
        /// resources required to run the template. Only set for cost estimation deployment
        /// types.
        /// </summary>
        public string CostEstimationCalculatorUrl { get; set; }
    }
}
