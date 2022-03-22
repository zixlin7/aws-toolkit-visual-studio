using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ECS.Models.Ecr;
using Amazon.ECR;
using Amazon.ECR.Model;

using Repository = Amazon.AWSToolkit.ECS.Models.Ecr.Repository;

namespace Amazon.AWSToolkit.ECS.PluginServices.Ecr
{
    public class RepoRepository : IRepoRepository
    {
        private readonly IAmazonECR _ecrClient;

        public RepoRepository(IAmazonECR ecrClient)
        {
            _ecrClient = ecrClient;
        }

        public async Task<IEnumerable<Repository>> GetRepositoriesAsync(CancellationToken token = default(CancellationToken))
        {
            var repos = new List<Repository>();

            var request = new DescribeRepositoriesRequest();
            do
            {
                var response = await _ecrClient.DescribeRepositoriesAsync(request);

                repos.AddRange(response.Repositories.Select(repo => repo.AsRepository()));

                request.NextToken = response.NextToken;

            } while (!string.IsNullOrWhiteSpace(request.NextToken));

            return repos;
        }
    }
}
