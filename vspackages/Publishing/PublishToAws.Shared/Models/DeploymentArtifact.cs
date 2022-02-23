namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Represents the central "thing" produced as the result of a deployment
    /// </summary>
    public enum DeploymentArtifact
    {
        CloudFormationStack,
        BeanstalkEnvironment,
        Unknown,
    }
}
