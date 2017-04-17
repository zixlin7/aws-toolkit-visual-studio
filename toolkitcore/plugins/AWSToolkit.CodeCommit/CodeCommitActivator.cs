using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using log4net;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.Nodes;
using Amazon.AWSToolkit.CodeCommit.Services;
using Amazon.AWSToolkit.Navigator;
using Amazon.CodeCommit;
using Amazon.CodeCommit.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using LibGit2Sharp;

namespace Amazon.AWSToolkit.CodeCommit
{
    public class CodeCommitActivator : AbstractPluginActivator, IAWSCodeCommit
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitActivator));
        private const string CodeCommitUrlPrefix = "git-codecommit.";

        public override string PluginName => "CodeCommit";

        public override void RegisterMetaNodes()
        {
            /*
            var rootMetaNode = new CodeCommitRootViewMetaNode();
            var repositoryMetaNode = new CodeCommitRepositoryViewMetaNode();

            rootMetaNode.Children.Add(repositoryMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
            */
        }

        void setupContextMenuHooks(CodeCommitRootViewMetaNode rootNode)
        {
            rootNode.CodeCommitRepositoryViewMetaNode.OnOpenRepositoryView =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewRepositoryController>().Execute);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSCodeCommit))
                return this;

            if (serviceType == typeof(IAWSToolkitGitServices))
                return new AWSToolkitGitServices(this);

            return null;
        }

        #region IAWSCodeCommit Members

        public IAWSToolkitGitServices ToolkitGitServices => new AWSToolkitGitServices(this);

        public void AssociateCredentialsWithProfile(string profileArtifactsId, string userName, string password)
        {
            ServiceSpecificCredentialStoreManager
                .Instance
                .SaveCredentialsForService(profileArtifactsId,
                    ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName,
                    userName,
                    password);
        }

        public ServiceSpecificCredentials CredentialsForProfile(string profileArtifactsId)
        {
            return
                ServiceSpecificCredentialStoreManager
                    .Instance
                    .GetCredentialsForService(profileArtifactsId,
                        ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName);
        }

        public ServiceSpecificCredentials ObtainGitCredentials(AccountViewModel account,
            RegionEndPointsManager.RegionEndPoints region)
        {
            var svcCredentials
                = ServiceSpecificCredentialStoreManager
                    .Instance
                    .GetCredentialsForService(account.SettingsUniqueKey,
                        ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName);

            if (svcCredentials != null)
                return svcCredentials;

            // nothing local, so first see if we can create credentials for the user if they
            // haven't already done so
            svcCredentials = ProbeIamForServiceSpecificCredentials(account, region);
            if (svcCredentials != null)
            {
                AssociateCredentialsWithProfile(account.SettingsUniqueKey, svcCredentials.Username,
                    svcCredentials.Password);
                return svcCredentials;
            }

            // can't autocreate due to use of root account, or no permissions, or they exist so final attempt 
            // is to get the user to perform the steps necessary to get credentials
            var registerCredentialsController = new RegisterServiceCredentialsController(account);
            return !registerCredentialsController.Execute().Success ? null : registerCredentialsController.Credentials;
        }

        public ICodeCommitRepository PromptForRepositoryToClone(AccountViewModel account,
            RegionEndPointsManager.RegionEndPoints initialRegion,
            string defaultCloneFolderRoot)
        {
            var controller = new SelectRepositoryController(account, initialRegion, defaultCloneFolderRoot);
            return !controller.Execute().Success
                ? null
                : new CodeCommitRepository(controller.Model.SelectedRepository, controller.Model.LocalFolder);
        }

        public INewCodeCommitRepositoryInfo PromptForRepositoryToCreate(AccountViewModel account,
            RegionEndPointsManager.RegionEndPoints initialRegion,
            string defaultFolderRoot)
        {
            var controller = new CreateRepositoryController(account, initialRegion, defaultFolderRoot);
            return !controller.Execute().Success
                ? null
                : controller.Model.GetNewRepositoryInfo();
        }

        public string PromptToSaveGeneratedCredentials(ServiceSpecificCredential generatedCredentials)
        {
            var controller = new SaveServiceSpecificCredentialsController(generatedCredentials);
            return controller.Execute().Success ? controller.SelectedFilename : null;
        }

        public bool IsCodeCommitRepository(string repoPath)
        {
            if (!Directory.Exists(repoPath))
                throw new ArgumentException("Specified repository path does not exist.");

            try
            {
                if (Repository.IsValid(repoPath))
                {
                    var repo = new Repository(repoPath);
                    var codeCommitRemoteUrl = FindCommitRemoteUrl(repo);
                    return !string.IsNullOrEmpty(codeCommitRemoteUrl);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Exception thrown from libgit2sharp while manipulating repository " + repoPath, e);
            }

            return false;
        }

        public IEnumerable<ICodeCommitRepository> GetRepositories(AccountViewModel account,
            IEnumerable<string> pathsToRepositories)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (pathsToRepositories == null)
                throw new ArgumentNullException(nameof(pathsToRepositories));

            var validRepositories = new List<ICodeCommitRepository>();

            var repositoryNameAndPathByRegion = GroupLocalRepositoriesByRegion(pathsToRepositories);

            foreach (var region in repositoryNameAndPathByRegion.Keys)
            {
                var client = BaseRepositoryModel.GetClientForRegion(account.Credentials, region);

                try
                {
                    var reposInRegion = repositoryNameAndPathByRegion[region];
                    var request = new BatchGetRepositoriesRequest
                    {
                        RepositoryNames = reposInRegion.Keys.ToList()
                    };
                    var batchGetResponse = client.BatchGetRepositories(request);
                    foreach (var repo in batchGetResponse.Repositories)
                    {
                        var wrapper = new CodeCommitRepository(repo)
                        {
                            LocalFolder = reposInRegion[repo.RepositoryName]
                        };

                        validRepositories.Add(wrapper);
                    }

                    if (batchGetResponse.RepositoriesNotFound != null)
                    {
                        foreach (var r in batchGetResponse.RepositoriesNotFound)
                        {
                            LOGGER.InfoFormat("Repository {0} was not found at the service during batch metadata query", r);
                        }
                    }
                }
                catch (Exception e)
                {
                    LOGGER.Error("Exception batch querying for repos in region " + region, e);
                }
            }

            return validRepositories;
        }

        public string GetConsoleBrowsingUrl(string repoPath)
        {
            var remoteUrl = FindCommitRemoteUrl(repoPath);
            if (string.IsNullOrEmpty(remoteUrl))
                return null;

            string consoleUrl = null;
            try
            {
                string repoName, region;
                ExtractRepoNameAndRegion(remoteUrl, out repoName, out region);

                // The hosts for remote (amazonaws.com) and console (aws.amazon.com) differ. 
                // As CodeCommit is not currently in any partition other than the global one 
                // this is safe for now.
                const string consoleUrlFormat = "https://{0}.console.aws.amazon.com/codecommit/home?region={0}#/repository/{1}/browse/HEAD/--/";
                return string.Format(consoleUrlFormat, region, repoName);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error attempting to form console url for repo at " + repoPath, e);
            }

            return consoleUrl;
        }

        #endregion

        /// <summary>
        /// If the user has no credentials for codecommit and their account is compatible, ask them
        /// if they'd like us to do the work on their behalf. If they decline, or the account isn't
        /// compatible, or auto-create fails, we'll display the regular dialog so they can paste in
        /// their credentials.
        /// </summary>
        /// <returns></returns>
        public ServiceSpecificCredentials ProbeIamForServiceSpecificCredentials(AccountViewModel account,
            RegionEndPointsManager.RegionEndPoints region)
        {
            const string iamEndpointsName = "IAM";
            const string codeCommitServiceName = "codecommit.amazonaws.com";
            const int maxServiceSpecificCredentials = 2;

            try
            {
                var iamConfig = new AmazonIdentityManagementServiceConfig
                {
                    ServiceURL = region.GetEndpoint(iamEndpointsName).Url
                };
                var iamClient = new AmazonIdentityManagementServiceClient(account.Credentials, iamConfig);

                // First, is the user running as an iam user or as root? If the latter, we can't help them
                var getUserResponse = iamClient.GetUser();
                if (string.IsNullOrEmpty(getUserResponse.User.UserName))
                {
                    LOGGER.InfoFormat(
                        "User profile {0} contains root credentials; cannot be used to create service-specific credentials.",
                        account.DisplayName);
                    return null;
                }

                var listCredentialsRequest = new ListServiceSpecificCredentialsRequest
                {
                    ServiceName = codeCommitServiceName,
                    UserName = getUserResponse.User.UserName
                };
                var listCredentialsReponse = iamClient.ListServiceSpecificCredentials(listCredentialsRequest);
                var credentialsExist =
                    listCredentialsReponse.ServiceSpecificCredentials.Any(ssc => ssc.Status == StatusType.Active);
                if (credentialsExist)
                {
                    LOGGER.InfoFormat(
                        "User profile {0} already has service-specific credentials for CodeCommit; user must import credentials",
                        account.DisplayName);
                    return null;
                }

                // IAM limits users to two sets of credentials - inactive credentials count against this limit, so 
                // if we already have two inactive sets, give up
                if (listCredentialsReponse.ServiceSpecificCredentials.Count == maxServiceSpecificCredentials)
                {
                    LOGGER.InfoFormat(
                        "User profile {0} already has the maximum amount of service-specific credentials for CodeCommit; user will have to activate and import credentials",
                        account.DisplayName);
                    return null;
                }

                // account is compatible, so let's see if the user wants us to go ahead
                const string msg = "Your account needs Git credentials to be generated to work with AWS CodeCommit. "
                                   +
                                   "The toolkit can try and create these credentials for you, and download them for you to save for future use. "
                                   + "\r\n"
                                   + "\r\n"
                                   + "Proceed to try and create credentials?";

                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Auto-create Git Credentials", msg,
                    MessageBoxButton.YesNo))
                    return null;

                // attempt to create a set of credentials and if successful, prompt the user to save them
                var createCredentialRequest = new CreateServiceSpecificCredentialRequest
                {
                    ServiceName = codeCommitServiceName,
                    UserName = getUserResponse.User.UserName
                };
                var createCredentialsResponse = iamClient.CreateServiceSpecificCredential(createCredentialRequest);
                var filename = PromptToSaveGeneratedCredentials(createCredentialsResponse.ServiceSpecificCredential);

                return ServiceSpecificCredentials.FromCsvFile(filename);
            }
            catch (Exception e)
            {
                LOGGER.Error("Exception while probing for CodeCommit credentials in IAM, user must supply manually", e);
            }

            return null;
        }

        private string FindCommitRemoteUrl(string repoPath)
        {
            try
            {
                if (Directory.Exists(repoPath) && Repository.IsValid(repoPath))
                {
                    return FindCommitRemoteUrl(new Repository(repoPath));
                }
            }
            catch
            {
            }

            return null;
        }

        private string FindCommitRemoteUrl(Repository repo)
        {
            var remotes = repo.Network?.Remotes;
            if (remotes != null && remotes.Any())
            {
                foreach (var remote in remotes)
                {
                    if (remote.Url.IndexOf(CodeCommitUrlPrefix, 0, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return remote.Url;
                    }
                }
            }

            return null;
        }

        private void ExtractRepoNameAndRegion(string codeCommitRemoteUrl, out string repoName, out string region)
        {
            // possibly fragile, but expecting host to be 'git-codecommit.REGION.suffix/.../reponame
            var serviceNameStart =
                codeCommitRemoteUrl.IndexOf(CodeCommitUrlPrefix, 0, StringComparison.OrdinalIgnoreCase);
            if (serviceNameStart == -1)
                throw new ArgumentException("Not a CodeCommit remote url");

            var regionStartPos =
                codeCommitRemoteUrl.IndexOf(".", serviceNameStart, StringComparison.OrdinalIgnoreCase) + 1;
            var regionEndPos = codeCommitRemoteUrl.IndexOf(".", regionStartPos, StringComparison.OrdinalIgnoreCase);

            region = codeCommitRemoteUrl.Substring(regionStartPos, regionEndPos - regionStartPos);

            var lastSlashPos = codeCommitRemoteUrl.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            repoName = codeCommitRemoteUrl.Substring(lastSlashPos + 1);
        }

        private Dictionary<string, Dictionary<string, string>> GroupLocalRepositoriesByRegion(IEnumerable<string> pathsToRepositories)
        {
            // associate the path with a repo name using a dictionary, so we get a fast lookup
            // when we're post-processing the batch metadata query which yields repo metadata by name
            var repositoryNameAndPathByRegion = new Dictionary<string, Dictionary<string, string>>();

            foreach (var path in pathsToRepositories)
            {
                // find the remote for CodeCommit, and from that we can infer the region and the
                // actual repo name. Trying to auto-discover this info without needing to perist
                // data about the repos we've seen
                if (!Repository.IsValid(path))
                    continue;

                var repo = new Repository(path);
                var codeCommitRemoteUrl = FindCommitRemoteUrl(repo);
                if (codeCommitRemoteUrl == null)
                    continue;

                string region;
                string repoName;
                ExtractRepoNameAndRegion(codeCommitRemoteUrl, out repoName, out region);

                if (repositoryNameAndPathByRegion.ContainsKey(region))
                {
                    repositoryNameAndPathByRegion[region].Add(repoName, path);
                }
                else
                {
                    var names = new Dictionary<string, string> { { repoName, path }};
                    repositoryNameAndPathByRegion.Add(region, names);
                }
            }

            return repositoryNameAndPathByRegion;
        }
    }
}
