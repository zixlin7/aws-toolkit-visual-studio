using System.Threading.Tasks;

using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public interface IInstanceRepository
    {
        Task<InstanceLog> GetInstanceLogAsync(string instanceInstanceId);
    }
}
