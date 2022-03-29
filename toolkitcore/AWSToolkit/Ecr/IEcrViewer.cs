using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.Ecr
{
    // This should not be here. This should be in the AWSToolkit.ECS.Interface assembly in the PluginInterfaces folder.
    // As a workaround to fix IDE-6946 this was put here until IDE-4975
    // can untangle the knot that is the homegrown plugin solution gone awry.
    public interface IEcrViewer
    {
        /// <summary>
        /// Views the specified ECR Repository in a document tab
        /// </summary>
        void ViewRepository(string repoName, ICredentialIdentifier identifier, ToolkitRegion region);
    }
}
