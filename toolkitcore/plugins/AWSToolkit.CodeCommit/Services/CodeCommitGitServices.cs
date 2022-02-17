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

        public CodeCommitGitServices(CodeCommitActivator hostActivator)
        {
            HostActivator = hostActivator;
        }

        private CodeCommitActivator HostActivator { get; }

        public Task CloneAsync(ServiceSpecificCredentials credentials, 
                                     string repositoryUrl, 
                                     string localFolder)
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
            catch (Exception e)
            {
                LOGGER.Error("Clone failed using libgit2sharp", e);

                var msg = string.Format("Failed to clone repository {0}. Error message: {1}.",
                    repositoryUrl,
                    e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Repository Clone Failed", msg);
            }

            return Task.FromResult<object>(null);
        }

        public async Task CreateAsync(INewCodeCommitRepositoryInfo newRepositoryInfo, 
                                      bool autoCloneNewRepository, 
                                      AWSToolkitGitCallbackDefinitions.PostCloneContentPopulationCallback contentPopulationCallback)
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
            catch (Exception e)
            {
                LOGGER.Error(e);
                throw;
            }

            // when called from within the VS package, local folder is not supplied so that
            // we can perform the clone through Team Explorer
            if (autoCloneNewRepository)
            {
                var svcCredentials
                    = newRepositoryInfo.OwnerAccount.GetCredentialsForService(ServiceSpecificCredentialStore.CodeCommitServiceName);
                try
                {
                    await CloneAsync(svcCredentials, newRepository.RepositoryUrl, newRepositoryInfo.LocalFolder);

                    newRepository.LocalFolder = newRepositoryInfo.LocalFolder;
                }
                catch (Exception e)
                {
                    LOGGER.Error("Exception cloning new repository", e);
                    throw new Exception("Error when attempting to clone the new repository", e);
                }

                var initialCommitContent = new List<string>();

                switch (newRepositoryInfo.GitIgnore.GitIgnoreType)
                {
                    case GitIgnoreOption.OptionType.VSToolkitDefault:
                    {
                        var content = S3FileFetcher.Instance.GetFileContent("CodeCommit/vsdefault.gitignore.txt",
                            S3FileFetcher.CacheMode.PerInstance);
                        var target = Path.Combine(newRepositoryInfo.LocalFolder, ".gitignore");
                        System.IO.File.WriteAllText(target, content);
                        initialCommitContent.Add(target);
                    }
                        break;

                    case GitIgnoreOption.OptionType.Custom:
                    {
                        var target = Path.Combine(newRepositoryInfo.LocalFolder, ".gitignore");
                        System.IO.File.Copy(newRepositoryInfo.GitIgnore.CustomFilename, target);
                        initialCommitContent.Add(target);
                    }
                        break;

                    case GitIgnoreOption.OptionType.None:
                        break;
                }

                if (contentPopulationCallback != null)
                {
                    var contentAdded = contentPopulationCallback(newRepository.LocalFolder);
                    if (contentAdded != null && contentAdded.Any())
                    {
                        foreach (var c in contentAdded)
                        {
                            initialCommitContent.Add(c);
                        }
                    }
                }

                if (initialCommitContent.Any())
                {
                    HostActivator.StageAndCommit(newRepositoryInfo.LocalFolder, initialCommitContent, "Initial commit", svcCredentials.Username);
                    HostActivator.Push(newRepositoryInfo.LocalFolder, svcCredentials);
                }
            }
        }
    }
}
