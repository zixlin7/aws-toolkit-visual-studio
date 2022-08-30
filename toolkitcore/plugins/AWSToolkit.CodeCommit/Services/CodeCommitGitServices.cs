using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit.Model;
using log4net;
using LibGit2Sharp;
using Amazon.AWSToolkit.CodeCommit.Interface;

namespace Amazon.AWSToolkit.CodeCommit.Services
{
    internal class CodeCommitGitServices : ICodeCommitGitServices
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitGitServices));

        public Task CloneAsync(ServiceSpecificCredentials credentials, string repositoryUrl, string localFolder)
        {
            try
            {
                CloneOptions cloneOptions = null;
                if (credentials != null)
                {
                    cloneOptions = new CloneOptions
                    {
                        CredentialsProvider =
                            (url, user, cred) =>
                                new UsernamePasswordCredentials
                                {
                                    Username = credentials.Username,
                                    Password = credentials.Password
                                }
                    };
                }

                Repository.Clone(repositoryUrl, localFolder, cloneOptions);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Clone failed using libgit2sharp", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Repository Clone Failed", $"Failed to clone repository {repositoryUrl}. Error message: {ex.Message}.");
            }

            return Task.FromResult<object>(null);
        }

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
