using Amazon.AWSToolkit.Publish.Models;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Util
{
    public static class DeploymentTypesExtensionMethods
    {
        public static DeploymentArtifact AsDeploymentArtifact(this DeploymentTypes deploymentTypes)
        {
            switch (deploymentTypes)
            {
                case DeploymentTypes.BeanstalkEnvironment:
                    return DeploymentArtifact.BeanstalkEnvironment;
                case DeploymentTypes.CloudFormationStack:
                    return DeploymentArtifact.CloudFormationStack;
                default:
                    return DeploymentArtifact.Unknown;
            }
        }
    }
}
