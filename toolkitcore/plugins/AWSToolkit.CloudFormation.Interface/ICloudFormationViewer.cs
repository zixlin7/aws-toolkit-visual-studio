using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CloudFormation
{
    public interface ICloudFormationViewer
    {
        void View(string stackName, ICredentialIdentifier identifier, ToolkitRegion region);
    }
}
