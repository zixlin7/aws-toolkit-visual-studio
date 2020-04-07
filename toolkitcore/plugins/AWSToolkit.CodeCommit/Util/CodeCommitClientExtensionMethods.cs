using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Util
{
    public static class CodeCommitClientExtensionMethods
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitClientExtensionMethods));
        private const int DefaultMaxBatchSize = 25;
        private const int MaxBatchShrinkFactor = 5;

        public static async Task<IList<string>> ListRepositoryNames(this IAmazonCodeCommit codeCommit)
        {
            var repositoryNames = new List<string>();
            var listRepositoriesRequest = new ListRepositoriesRequest()
            {
                NextToken = null
            };

            do
            {
                try
                {
                    var response = await codeCommit.ListRepositoriesAsync(listRepositoriesRequest);

                    repositoryNames.AddRange(response.Repositories.Select(x => x.RepositoryName));
                    listRepositoriesRequest.NextToken = response.NextToken;
                }
                catch (Exception e)
                {
                    LOGGER.Error("Call to CodeCommit ListRepositories failed", e);
                    break;
                }
            } while (!string.IsNullOrEmpty(listRepositoriesRequest.NextToken));

            return repositoryNames;
        }

        public static async Task<IList<RepositoryMetadata>> GetRepositoryMetadata(
            this IAmazonCodeCommit codeCommit,
            IList<string> repositoryNames,
            int maxBatchSize = DefaultMaxBatchSize)
        {
            var metadata = new List<RepositoryMetadata>();
            var failedRepositoryNames = new List<string>();

            // Get Repository information in controlled batch sizes
            var chunkedNames = repositoryNames.Split(maxBatchSize);

            var tasks = chunkedNames.Select(names => Task.Run(async () =>
            {
                try
                {
                    var response = await codeCommit.BatchGetRepositoriesAsync(new BatchGetRepositoriesRequest
                    {
                        RepositoryNames = names
                    });

                    response.RepositoriesNotFound?.ForEach(repositoryName =>
                    {
                        LOGGER.InfoFormat("Repository {0} was not found at the service during batch metadata query", repositoryName);
                    });

                    metadata.AddRange(response.Repositories);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Call to CodeCommit BatchGetRepositories failed", e);
                    failedRepositoryNames.AddRange(names);
                }
            }));

            await Task.WhenAll(tasks);

            // Workaround for CodeCommit rejecting a whole batch as bad...
            // Try retrieving smaller batches of anything that failed, so the user sees good repos
            if (failedRepositoryNames.Any() && maxBatchSize > 1)
            {
                while (maxBatchSize > failedRepositoryNames.Count)
                {
                    maxBatchSize = Math.Max(1, maxBatchSize / MaxBatchShrinkFactor);
                }

                metadata.AddRange(await codeCommit.GetRepositoryMetadata(failedRepositoryNames, maxBatchSize));
            }

            return metadata;
        }
    }
}