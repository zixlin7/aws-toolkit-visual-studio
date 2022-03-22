using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.ECS.PluginServices.Ecr
{
    public interface IRepositoryFactory
    {
        IRepoRepository CreateRepoRepository(ICredentialIdentifier credentialsId, ToolkitRegion region);
    }
}
