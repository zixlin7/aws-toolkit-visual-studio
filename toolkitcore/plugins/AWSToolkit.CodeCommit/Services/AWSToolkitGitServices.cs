using System;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit.Model;
using log4net;
using LibGit2Sharp;

namespace Amazon.AWSToolkit.CodeCommit.Services
{
    internal class AWSToolkitGitServices : IAWSToolkitGitServices
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSToolkitGitServices));

        public AWSToolkitGitServices(CodeCommitActivator hostActivator)
        {
            HostActivator = hostActivator;
        }

        private CodeCommitActivator HostActivator { get; }

        public void Clone(ServiceSpecificCredentials credentials, string repositoryUrl, string localFolder)
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
        }

        public object Create(AccountViewModel account,
                             RegionEndPointsManager.RegionEndPoints region,
                             string name,
                             string description,
                             string localFolder,
                             AWSToolkitGitCallbackDefinitions.PostCloneContentPopulationCallback contentPopulationCallback)
        {
            CodeCommitRepository newRepository;

            try
            {
                var client = BaseRepositoryModel.GetClientForRegion(account.Credentials, region.SystemName);

                var request = new CreateRepositoryRequest
                {
                    RepositoryName = name,
                    RepositoryDescription = description
                };
                var response = client.CreateRepository(request);

                newRepository = new CodeCommitRepository(response.RepositoryMetadata);
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
                throw new Exception("Service error received creating repository", e);
            }


            // when called from within the VS package, local folder is not supplied so that
            // we can perform the clone through Team Explorer
            if (!string.IsNullOrEmpty(localFolder))
            {
                try
                {
                    var svcCredentials 
                        = account.GetCredentialsForService(ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName);
                    Clone(svcCredentials, newRepository.RepositoryUrl, localFolder);

                    newRepository.LocalFolder = localFolder;
                }
                catch (Exception e)
                {
                    LOGGER.Error("Exception cloning new repository", e);
                    throw new Exception("Error when attempting to clone the new repository", e);
                }    
            }

            if (contentPopulationCallback != null)
            {
                var contentAdded = contentPopulationCallback(newRepository.LocalFolder);
                if (contentAdded)
                {
                    // todo
                }
            }

            return newRepository;
        }
    }
}
