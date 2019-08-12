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
