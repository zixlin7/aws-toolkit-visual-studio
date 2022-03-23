using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ECS.Models.Ecr;

namespace Amazon.AWSToolkit.ECS.PluginServices.Ecr
{
    public interface IRepoRepository
    {
        Task<IEnumerable<Repository>> GetRepositoriesAsync(CancellationToken token = default(CancellationToken));
    }
}
