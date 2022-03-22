using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;

namespace Amazon.AWSToolkit.ECS.PluginServices.Ecr
{
    public class RepositoryFactory : IRepositoryFactory
    {
        private readonly ToolkitContext _toolkitContext;

        public RepositoryFactory(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public IRepoRepository CreateRepoRepository(ICredentialIdentifier credentialsId, ToolkitRegion region)
        {
            IAmazonECR ecrClient = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonECRClient>(credentialsId, region);
            return new RepoRepository(ecrClient);
        }
    }
}
