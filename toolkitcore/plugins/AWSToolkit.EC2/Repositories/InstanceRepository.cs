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
    }
}
