using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public class ElasticIpRepository : IElasticIpRepository
    {
        private readonly IAmazonEC2 _ec2;

        public ElasticIpRepository(IAmazonEC2 ec2)
        {
            _ec2 = ec2;
        }

        public async Task<IEnumerable<AddressWrapper>> ListElasticIpsAsync()
        {
            var response = await _ec2.DescribeAddressesAsync(new DescribeAddressesRequest());

            return response.Addresses.Select(a => new AddressWrapper(a));
        }

        public async Task<string> AllocateElasticIpAsync(string domain)
        {
            var request = new AllocateAddressRequest() { Domain = domain };
            var response = await _ec2.AllocateAddressAsync(request);

            return response.PublicIp;
        }

        public async Task ReleaseElasticIpAsync(AddressWrapper address)
        {
            ReleaseAddressRequest request = null;

            if (address.NativeAddress.Domain == AddressWrapper.DOMAIN_EC2)
            {
                request = new ReleaseAddressRequest() { PublicIp = address.PublicIp };
            }
            else
            {
                request = new ReleaseAddressRequest() { AllocationId = address.AllocationId };
            }

            await _ec2.ReleaseAddressAsync(request);
        }

        public async Task AssociateWithInstance(AddressWrapper address, string instanceId)
        {
            var request = new AssociateAddressRequest
            {
                InstanceId = instanceId,
            };

            if (address.Domain == AddressWrapper.DOMAIN_EC2)
            {
                request.PublicIp = address.NativeAddress.PublicIp;
            }
            else
            {
                request.AllocationId = address.NativeAddress.AllocationId;
            }

            await _ec2.AssociateAddressAsync(request);
        }

        public async Task DisassociateFromInstanceAsync(AddressWrapper address)
        {
            DisassociateAddressRequest request = null;

            if (address.NativeAddress.Domain == AddressWrapper.DOMAIN_EC2)
            {
                request = new DisassociateAddressRequest() { PublicIp = address.PublicIp };
            }
            else
            {
                request = new DisassociateAddressRequest() { AssociationId = address.AssociationId };
            }

            await _ec2.DisassociateAddressAsync(request);
        }

        public async Task<IEnumerable<AssociateAddressModel.InstanceItem>> GetUnassociatedInstancesAsync(string domain)
        {
            var addressResponseTask = _ec2.DescribeAddressesAsync(new DescribeAddressesRequest());
            var instanceResponseTask = _ec2.DescribeInstancesAsync(new DescribeInstancesRequest());

            await Task.WhenAll(addressResponseTask, instanceResponseTask);

            // Current Elastic IPs
            var publicIps = addressResponseTask.Result.Addresses.Select(x => x.PublicIp).ToHashSet();

            // Unassociated instances
            return instanceResponseTask.Result.Reservations
                .SelectMany(reservation => reservation.Instances)
                .Where(instance =>
                    instance.State.Name != EC2Constants.INSTANCE_STATE_SHUTTING_DOWN &&
                    instance.State.Name != EC2Constants.INSTANCE_STATE_TERMINATED &&
                    !publicIps.Contains(instance.PublicIpAddress))
                .Where(instance =>
                    domain == AddressWrapper.DOMAIN_EC2 && string.IsNullOrEmpty(instance.SubnetId) ||
                    domain == AddressWrapper.DOMAIN_VPC && !string.IsNullOrEmpty(instance.SubnetId))
                .Select(instance => new AssociateAddressModel.InstanceItem(instance))
                .ToList();
        }
    }
}
