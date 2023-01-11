using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Navigator;
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

        private IAmazonEC2 CreateEc2Client(AwsConnectionSettings awsConnectionSettings) =>
            _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(awsConnectionSettings);
    }
}
