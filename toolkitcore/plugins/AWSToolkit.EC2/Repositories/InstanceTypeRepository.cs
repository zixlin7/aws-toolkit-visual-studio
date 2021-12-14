using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public class InstanceTypeRepository : IInstanceTypeRepository
    {
        private readonly ToolkitContext _toolkitContext;

        public InstanceTypeRepository(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public async Task<ICollection<InstanceTypeModel>> ListInstanceTypesAsync(ICredentialIdentifier credentialsId,
            ToolkitRegion region)
        {
            var ec2 = _toolkitContext.ServiceClientManager.CreateServiceClient<AmazonEC2Client>(credentialsId, region);

            var instanceTypes = new List<InstanceTypeModel>();

            var request = new DescribeInstanceTypesRequest();
            do
            {
                var response = await ec2.DescribeInstanceTypesAsync(request).ConfigureAwait(false);
                request.NextToken = response.NextToken;

                instanceTypes.AddRange(response.InstanceTypes.Select(CreateInstanceType));
            } while (!string.IsNullOrWhiteSpace(request.NextToken));

            return instanceTypes;
        }

        private InstanceTypeModel CreateInstanceType(InstanceTypeInfo instanceTypeInfo)
        {
            var instanceType = new InstanceTypeModel()
            {
                Id = instanceTypeInfo.InstanceType.Value,
                VirtualCpus = instanceTypeInfo.VCpuInfo.DefaultVCpus,
                MemoryMib = instanceTypeInfo.MemoryInfo.SizeInMiB,
                StorageGb = instanceTypeInfo.InstanceStorageInfo?.TotalSizeInGB ?? 0,
                Architectures = instanceTypeInfo.ProcessorInfo.SupportedArchitectures,
            };

            return instanceType;
        }
    }
}
