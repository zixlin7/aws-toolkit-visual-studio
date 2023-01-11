using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public class ElasticIpRepository : IElasticIpRepository
    {
        private readonly ToolkitContext _toolkitContext;

        public ElasticIpRepository(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task<IEnumerable<AddressWrapper>> ListElasticIpsAsync(AwsConnectionSettings awsConnectionSettings)
        {
            var ec2 = CreateEc2Client(awsConnectionSettings);

            var response = await ec2.DescribeAddressesAsync(new DescribeAddressesRequest());

            return response.Addresses.Select(a => new AddressWrapper(a));
        }

        public async Task<string> AllocateElasticIpAsync(string domain, AwsConnectionSettings awsConnectionSettings)
        {
            var ec2 = CreateEc2Client(awsConnectionSettings);

            var request = new AllocateAddressRequest() { Domain = domain };
            var response = await ec2.AllocateAddressAsync(request);

            return response.PublicIp;
        }

        public async Task ReleaseElasticIpAsync(AddressWrapper address, AwsConnectionSettings awsConnectionSettings)
        {
            var ec2 = CreateEc2Client(awsConnectionSettings);

            ReleaseAddressRequest request = null;

            if (address.NativeAddress.Domain == AddressWrapper.DOMAIN_EC2)
            {
                request = new ReleaseAddressRequest() { PublicIp = address.PublicIp };
            }
            else
            {
                request = new ReleaseAddressRequest() { AllocationId = address.AllocationId };
            }

            await ec2.ReleaseAddressAsync(request);
        }

        public async Task AssociateWithInstance(AddressWrapper address, string instanceId, AwsConnectionSettings awsConnectionSettings)
        {
            var ec2 = CreateEc2Client(awsConnectionSettings);

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

            await ec2.AssociateAddressAsync(request);
        }

        public async Task<IEnumerable<AssociateAddressModel.InstanceItem>> GetUnassociatedInstancesAsync(string domain, AwsConnectionSettings awsConnectionSettings)
        {
            var ec2 = CreateEc2Client(awsConnectionSettings);

            var addressResponseTask = ec2.DescribeAddressesAsync(new DescribeAddressesRequest());
            var instanceResponseTask = ec2.DescribeInstancesAsync(new DescribeInstancesRequest());

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

        private IAmazonEC2 CreateEc2Client(AwsConnectionSettings awsConnectionSettings) =>
            _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(awsConnectionSettings);
    }
}
