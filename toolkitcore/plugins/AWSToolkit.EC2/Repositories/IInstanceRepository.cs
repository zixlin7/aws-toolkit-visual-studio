using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Repositories
{
    public interface IInstanceRepository
    {
        Task<IEnumerable<RunningInstanceWrapper>> ListInstancesAsync();
        Task<InstanceLog> GetInstanceLogAsync(string instanceInstanceId);
        Task<string> CreateImageFromInstanceAsync(string instanceId, string imageName, string imageDescription);
        Task<bool> IsTerminationProtectedAsync(string instanceId);
        Task SetTerminationProtectionAsync(string instanceId, bool enabled);
        Task<string> GetUserDataAsync(string instanceId);
        Task SetUserDataAsync(string instanceId, string userData);
        Task<IEnumerable<InstanceType>> GetSupportingInstanceTypes(string imageId);
        Task UpdateInstanceTypeAsync(string instanceId, string instanceTypeId);
        Task<string> GetShutdownBehaviorAsync(string instanceId);
        Task SetShutdownBehaviorAsync(string instanceId, string behavior);
        Task AssociateWithNewElasticIpAsync(string instanceId, string instanceVpcId, string domain);
        Task AssociateWithElasticIpAsync(string instanceId, string instanceVpcId, string publicIp, string allocationId);
        Task DisassociateElasticIpAsync(RunningInstanceWrapper instance);
    }
}
