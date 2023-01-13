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
    }
}
