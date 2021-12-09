using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2
{
    public class VpcRepository : IVpcRepository
    {
        private readonly ToolkitContext _toolkitContext;

        public VpcRepository(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task<ICollection<VpcEntity>> ListVpcsAsync(ICredentialIdentifier credentialsId, ToolkitRegion region)
        {
            var ec2 = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(credentialsId, region);

            var vpcs = new List<VpcEntity>();

            var request = new DescribeVpcsRequest();
            do
            {
                var response = await ec2.DescribeVpcsAsync(request).ConfigureAwait(false);
                request.NextToken = response.NextToken;

                vpcs.AddRange(response.Vpcs.Select(CreateVpcEntity));
            } while (!string.IsNullOrWhiteSpace(request.NextToken));

            return vpcs;
        }

        private VpcEntity CreateVpcEntity(Vpc vpc)
        {
            var vpcEntity = new VpcEntity()
            {
                Id = vpc.VpcId,
                IsDefault = vpc.IsDefault,
                Name = vpc.Tags?.FirstOrDefault(x => x.Key == "Name")?.Value ?? String.Empty,
            };

            return vpcEntity;
        }
    }
}
