using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public interface IElasticIpRepository
    {
        Task<IEnumerable<AddressWrapper>> ListElasticIpsAsync();

        /// <summary>
        /// Allocates (creates) an Elastic IP
        /// </summary>
        /// <returns>Public IP of the created Elastic IP</returns>
        Task<string> AllocateElasticIpAsync(string domain);

        Task ReleaseElasticIpAsync(AddressWrapper address);

        Task AssociateWithInstance(AddressWrapper address, string instanceId);

        Task<IEnumerable<AssociateAddressModel.InstanceItem>> GetUnassociatedInstancesAsync(string domain);

        Task DisassociateFromInstanceAsync(AddressWrapper address);
    }
}
