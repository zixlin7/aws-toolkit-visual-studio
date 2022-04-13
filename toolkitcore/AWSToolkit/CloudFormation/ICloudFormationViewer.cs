using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudFormation
{
    // This should not be here. This should be in the AWSToolkit.CloudFormation.Interface assembly in the PluginInterfaces folder.
    // As a workaround to fix IDE-6946 this was put here until IDE-4975
    // can untangle the knot that is the homegrown plugin solution gone awry.
    public interface ICloudFormationViewer
    {
        void View(string stackName, AwsConnectionSettings connectionSettings);
    }
}
