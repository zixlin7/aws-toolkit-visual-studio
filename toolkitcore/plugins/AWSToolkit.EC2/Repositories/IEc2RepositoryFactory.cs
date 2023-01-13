using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public interface IEc2RepositoryFactory
    {
        IInstanceRepository CreateInstanceRepository(AwsConnectionSettings awsConnectionSettings);
    }
}
