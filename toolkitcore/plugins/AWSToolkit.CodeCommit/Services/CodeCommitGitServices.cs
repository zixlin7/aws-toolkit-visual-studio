using System;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.CodeCommit.Model;

using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Services
{
    internal class CodeCommitGitServices : ICodeCommitGitServices
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitGitServices));

        public async Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo)
        {
            CodeCommitRepository newRepository;

            try
            {
                var client = BaseRepositoryModel.GetClientForRegion(newRepositoryInfo.OwnerAccount, newRepositoryInfo.Region);

                var request = new CreateRepositoryRequest
                {
                    RepositoryName = newRepositoryInfo.Name,
                    RepositoryDescription = newRepositoryInfo.Description
                };
                var response = client.CreateRepository(request);

                newRepository = new CodeCommitRepository(response.RepositoryMetadata);
            }
            catch (Exception ex)
            {
                LOGGER.Error(ex);
                throw;
            }
        }
    }
}
