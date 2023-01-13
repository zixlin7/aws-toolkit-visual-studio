using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public class InstanceRepository : IInstanceRepository
    {
        private readonly IAmazonEC2 _ec2;

        public InstanceRepository(IAmazonEC2 ec2)
        {
            _ec2 = ec2;
        }

        public async Task<IEnumerable<RunningInstanceWrapper>> ListInstancesAsync()
        {
            var listIpsTask = _ec2.DescribeAddressesAsync(new DescribeAddressesRequest());
            var listInstancesTask = _ec2.DescribeInstancesAsync(new DescribeInstancesRequest());

            await Task.WhenAll(listIpsTask, listInstancesTask);

            var instanceIdToIp = new Dictionary<string, AddressWrapper>();
            listIpsTask.Result.Addresses
                .Where(address => !string.IsNullOrWhiteSpace(address.InstanceId))
                .ToList()
                .ForEach(address => instanceIdToIp[address.InstanceId] = new AddressWrapper(address));

            return listInstancesTask.Result.Reservations
                .SelectMany(reservation => reservation.Instances.Select(instance =>
                {
                    instanceIdToIp.TryGetValue(instance.InstanceId, out var address);
                    return new RunningInstanceWrapper(reservation, instance, address);
                })).ToList();
        }

        public async Task<InstanceLog> GetInstanceLogAsync(string instanceId)
        {
            var response = await _ec2.GetConsoleOutputAsync(new GetConsoleOutputRequest() { InstanceId = instanceId });

            return new InstanceLog { Timestamp = response.Timestamp, Log = response.Output, };
        }

        public async Task<string> CreateImageFromInstanceAsync(string instanceId, string imageName, string imageDescription)
        {
            var request = new CreateImageRequest()
            {
                InstanceId = instanceId,
                Name = imageName,
                Description = imageDescription,
            };

            var response = await _ec2.CreateImageAsync(request);

            return response.ImageId;
        }

        public async Task<bool> IsTerminationProtectedAsync(string instanceId)
        {
            var request = new DescribeInstanceAttributeRequest()
            {
                InstanceId = instanceId,
                Attribute = InstanceAttributeName.DisableApiTermination,
            };

            var response = await _ec2.DescribeInstanceAttributeAsync(request);

            return response.InstanceAttribute.DisableApiTermination;
        }

        public async Task SetTerminationProtectionAsync(string instanceId, bool enabled)
        {
            var request = new ModifyInstanceAttributeRequest()
            {
                InstanceId = instanceId,
                Attribute = InstanceAttributeName.DisableApiTermination,
                Value = enabled.ToString(),
            };

            await _ec2.ModifyInstanceAttributeAsync(request);
        }

        public async Task<string> GetUserDataAsync(string instanceId)
        {
            var request = new DescribeInstanceAttributeRequest()
            {
                InstanceId = instanceId,
                Attribute = InstanceAttributeName.UserData,
            };

            var response = await _ec2.DescribeInstanceAttributeAsync(request);

            return response.InstanceAttribute.UserData;
        }

        public async Task SetUserDataAsync(string instanceId, string userData)
        {
            var request = new ModifyInstanceAttributeRequest()
            {
                InstanceId = instanceId,
                UserData = userData,
            };

            await _ec2.ModifyInstanceAttributeAsync(request);
        }

        public async Task<IEnumerable<InstanceType>> GetSupportingInstanceTypes(string imageId)
        {
            var request = new DescribeImagesRequest() { ImageIds = new List<string>() { imageId } };
            var response = await _ec2.DescribeImagesAsync(request);

            if (response.Images.Count != 1)
            {
                throw new Ec2Exception($"Unable to find details about Image {imageId}. Images found: {response.Images.Count}", Ec2Exception.Ec2ErrorCode.NoImages);
            }

            return InstanceType.GetValidTypes(response.Images.Single());
        }

        public async Task UpdateInstanceTypeAsync(string instanceId, string instanceTypeId)
        {
            var request = new ModifyInstanceAttributeRequest()
            {
                InstanceId = instanceId,
                Attribute = InstanceAttributeName.InstanceType,
                Value = instanceTypeId,
            };

            await _ec2.ModifyInstanceAttributeAsync(request);
        }
    }
}
