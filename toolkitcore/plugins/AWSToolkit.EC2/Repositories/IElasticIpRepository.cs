using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public interface IElasticIpRepository
    {
        Task<IEnumerable<AddressWrapper>> ListElasticIpsAsync(AwsConnectionSettings awsConnectionSettings);

        /// <summary>
        /// Allocates (creates) an Elastic IP
        /// </summary>
        /// <returns>Public IP of the created Elastic IP</returns>
        Task<string> AllocateElasticIpAsync(string domain, AwsConnectionSettings awsConnectionSettings);

        Task ReleaseElasticIpAsync(AddressWrapper address, AwsConnectionSettings awsConnectionSettings);
    }
}
