using System;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
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

        private CodeCommitActivator HostActivator { get; set; }

        public void Clone(string repositoryUrl, string destinationFolder, AccountViewModel account)
        {
            try
            {
                var credentials = ServiceSpecificCredentialStoreManager
                                    .Instance
                                    .GetCredentialsForService(account.SettingsUniqueKey, CodeCommitConstants.CodeCommitServiceCredentialsName);
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

                Repository.Clone(repositoryUrl, destinationFolder, cloneOptions);
            }
            catch (Exception e)
            {
                // todo: need to display in the UI too
                var msg = string.Format("Failed to clone repository {0} using libgit2sharp. Exception message {1}.",
                                        repositoryUrl, 
                                        e.Message);
                LOGGER.Error(msg, e);
            }
        }
    }
}
