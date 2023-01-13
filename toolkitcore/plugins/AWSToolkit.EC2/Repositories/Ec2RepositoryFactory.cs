using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public class Ec2RepositoryFactory : IEc2RepositoryFactory
    {
        private readonly ToolkitContext _toolkitContext;

        public Ec2RepositoryFactory(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public IInstanceRepository CreateInstanceRepository(AwsConnectionSettings awsConnectionSettings)
        {
            IAmazonEC2 ec2 = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(awsConnectionSettings);
            return new InstanceRepository(ec2);
        }
    }
}
