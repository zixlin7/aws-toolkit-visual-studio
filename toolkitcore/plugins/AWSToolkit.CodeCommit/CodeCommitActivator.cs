using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.Services;
using Amazon.AWSToolkit.CodeCommit.Util;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCommit.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using LibGit2Sharp;

using log4net;

namespace Amazon.AWSToolkit.CodeCommit
{
    public class CodeCommitActivator : AbstractPluginActivator, IAWSCodeCommit
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitActivator));

        private ToolkitRegion _fallbackRegion;
        private const string CodeCommitUrlPrefix = "git-codecommit.";

        public override string PluginName => "CodeCommit";

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSCodeCommit))
            {
                return this;
            }

            if (serviceType == typeof(IAWSToolkitGitServices))
            {
                return new CodeCommitGitServices(this);
            }

            return null;
        }

        #region IAWSCodeCommit Members

        public ICodeCommitGitServices CodeCommitGitServices => new CodeCommitGitServices(this);

        public void AssociateCredentialsWithProfile(string profileArtifactsId, string userName, string password)
        {
            ServiceSpecificCredentialStore
                .Instance
                .SaveCredentialsForService(profileArtifactsId,
                    ServiceSpecificCredentialStore.CodeCommitServiceName,
                    userName,
                    password);
        }

        public ServiceSpecificCredentials CredentialsForProfile(string profileArtifactsId)
        {
            return
                ServiceSpecificCredentialStore
                    .Instance
                    .GetCredentialsForService(profileArtifactsId,
                        ServiceSpecificCredentialStore.CodeCommitServiceName);
        }

        public ServiceSpecificCredentials ObtainGitCredentials(AccountViewModel account,
                                                               ToolkitRegion region,
                                                               bool ignoreCurrent)
        {
            ServiceSpecificCredentials svcCredentials;

            if (!ignoreCurrent)
            {
                svcCredentials = ServiceSpecificCredentialStore
                                    .Instance
                                    .GetCredentialsForService(account.SettingsUniqueKey,
                                        ServiceSpecificCredentialStore.CodeCommitServiceName);

                if (svcCredentials != null)
                {
                    return svcCredentials;
                }
            }

            // Nothing local, so first see if we can create credentials for the user if they
            // haven't already done so
            svcCredentials = ProbeIamForServiceSpecificCredentials(account, region);
            if (svcCredentials != null)
            {
                AssociateCredentialsWithProfile(account.SettingsUniqueKey, svcCredentials.Username, svcCredentials.Password);
                return svcCredentials;
            }

            // Can't autocreate due to use of root account, or no permissions, or they exist so final attempt 
            // is to get the user to perform the steps necessary to get credentials
            var registerCredentialsController = new RegisterServiceCredentialsController(account);
            var results = registerCredentialsController.Execute().Success ? registerCredentialsController.Credentials : null;

            return results;
        }

        public ICodeCommitRepository PromptForRepositoryToClone(AccountViewModel account, ToolkitRegion initialRegion, string defaultFolderRoot)
        {
            initialRegion = initialRegion ?? GetFallbackRegion();
            var controller = new CloneRepositoryController(account, initialRegion, defaultFolderRoot);
            return controller.Execute().Success ?
                new CodeCommitRepository(controller.Model.SelectedRepository, controller.Model.SelectedFolder) :
                null;
        }

        public INewCodeCommitRepositoryInfo PromptForRepositoryToCreate(AccountViewModel account, ToolkitRegion initialRegion, string defaultFolderRoot)
        {
            initialRegion = initialRegion ?? GetFallbackRegion();
            var controller = new CreateRepositoryController(account, initialRegion, defaultFolderRoot);
            return controller.Execute().Success ? controller.Model.GetNewRepositoryInfo() : null;
        }

        public string PromptToSaveGeneratedCredentials(ServiceSpecificCredential generatedCredentials, string msg = null)
        {
            var controller = new SaveServiceSpecificCredentialsController(generatedCredentials, msg);
            return controller.Execute().Success ? controller.SelectedFilename : null;
        }

        public bool IsCodeCommitRepository(string repoPath)
        {
            ThrowOnInvalidRepo(repoPath);

            try
            {
                using (var repo = new Repository(repoPath))
                {
                    return !string.IsNullOrEmpty(FindCommitRemoteUrl(repo));
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Exception thrown from libgit2sharp while manipulating repository {repoPath}", ex);
            }

            return false;
        }

        public ToolkitRegion GetRepositoryRegion(string repoPath)
        {
            ThrowOnInvalidRepo(repoPath);

            ExtractRepoNameAndRegion(FindCommitRemoteUrl(repoPath), out var _, out ToolkitRegion region);
            return region;
        }

        public async Task<IEnumerable<ICodeCommitRepository>> GetRepositories(AccountViewModel account, IEnumerable<string> pathsToRepositories)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account));
            }

            if (pathsToRepositories == null)
            {
                throw new ArgumentNullException(nameof(pathsToRepositories));
            }

            var validRepositories = new List<ICodeCommitRepository>();
            var repositoryNameAndPathByRegion = GroupLocalRepositoriesByRegion(pathsToRepositories);

            // Load local Repos for each region
            var tasks = repositoryNameAndPathByRegion.Keys.Select(async regionId =>
            {
                var region = ToolkitContext?.RegionProvider.GetRegion(regionId);
                if (region != null)
                {
                    validRepositories.AddRange(await LoadLocalReposForRegion(region, account, repositoryNameAndPathByRegion));
                }
            });

            await Task.WhenAll(tasks);

            // This reorders across all regions; we may eventually decide to group by region eventually in the UI
            return validRepositories
                .OrderBy(x => x.Name)
                .ThenBy(x => x.LocalFolder);
        }

        private static async Task<IList<ICodeCommitRepository>> LoadLocalReposForRegion(
            ToolkitRegion region,
            AccountViewModel account, 
            Dictionary<string, Dictionary<string, List<string>>> repositoryNameAndPathByRegion)
        {
            List<ICodeCommitRepository> validRepositories = new List<ICodeCommitRepository>();

            try
            {
                var client = BaseRepositoryModel.GetClientForRegion(account, region);
                var repoNameToLocalPaths = repositoryNameAndPathByRegion[region.Id];
                var repositoryMetadatas = await client.GetRepositoryMetadata(repoNameToLocalPaths.Keys.ToList());

                foreach (var repo in repositoryMetadatas)
                {
                    var repoLocalPaths = repoNameToLocalPaths[repo.RepositoryName];
                    var models = repoLocalPaths.Select(localPath => new CodeCommitRepository(repo) { LocalFolder = localPath });
                    validRepositories.AddRange(models);
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Exception batch querying for repos in region {region?.Id}", ex);
            }

            return validRepositories;
        }

        public ICodeCommitRepository GetRepository(string repositoryName, AccountViewModel account, ToolkitRegion region)
        {
            try
            {
                var client = BaseRepositoryModel.GetClientForRegion(account, region);
                var response = client.GetRepository(new GetRepositoryRequest {RepositoryName = repositoryName});
                return new CodeCommitRepository(response.RepositoryMetadata);
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Error querying for repository {repositoryName}", ex);
            }

            return null;
        }

        public string GetConsoleBrowsingUrl(string repoPath)
        {
            ThrowOnInvalidRepo(repoPath);

            var remoteUrl = FindCommitRemoteUrl(repoPath);
            if (string.IsNullOrEmpty(remoteUrl))
            {
                return null;
            }

            string consoleUrl = null;
            try
            {
                ExtractRepoNameAndRegion(remoteUrl, out string repoName, out string region);

                // The hosts for remote (amazonaws.com) and console (aws.amazon.com) differ. 
                // As CodeCommit is not currently in any partition other than the global one this is safe for now.
                return $"https://{region}.console.aws.amazon.com/codecommit/home?region={region}#/repository/{repoName}/browse/HEAD/--/";
            }
            catch (Exception ex)
            {
                LOGGER.Error($"Error attempting to form console url for repo at {repoPath}", ex);
            }

            return consoleUrl;
        }

        public bool StageAndCommit(string repoPath, IEnumerable<string> files, string commitMessage, string userName)
        {
            ThrowOnInvalidRepo(repoPath);

            try
            {
                using (var repo = new Repository(repoPath))
                {
                    var relativeFiles = new List<string>();
                    var rootedRepoPath = repoPath.EndsWith(@"\") ? repoPath : $@"{repoPath}\";
                    foreach (var f in files)
                    {
                        if (f.StartsWith(rootedRepoPath, StringComparison.OrdinalIgnoreCase))
                        {
                            relativeFiles.Add(f.Substring(rootedRepoPath.Length));
                        }
                        else
                        {
                            relativeFiles.Add(f);
                        }
                    }

                    repo.Stage(relativeFiles);

                    var author = new Signature(userName, userName, DateTime.Now);
                    var committer = author;

                    // Commit to the repository
                    repo.Commit(commitMessage, author, committer);
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Exception during staging and commit", ex);
            }

            return false;
        }

        public bool Push(string repoPath, ServiceSpecificCredentials credentials)
        {
            ThrowOnInvalidRepo(repoPath);

            try
            {
                using (var repo = new Repository(repoPath))
                {
                    var remote = FindCodeCommitRemote(repo);
                    var options = new PushOptions
                    {
                        CredentialsProvider = (url, user, cred) =>
                            new UsernamePasswordCredentials
                            {
                                Username = credentials.Username,
                                Password = credentials.Password
                            }
                    };
                    repo.Network.Push(remote, "refs/heads/master", options);
                }

                return true;
            }
            catch (Exception ex)
            {
                LOGGER.Error("Exception during push", ex);
            }

            return false;
        }

        #endregion

        const string codeCommitServiceName = "codecommit.amazonaws.com";

        /// <summary>
        /// If the user has no credentials for codecommit and their account is compatible, ask them
        /// if they'd like us to do the work on their behalf. If they decline, or the account isn't
        /// compatible, or auto-create fails, we'll display the regular dialog so they can paste in
        /// their credentials.
        /// </summary>
        /// <returns></returns>
        public ServiceSpecificCredentials ProbeIamForServiceSpecificCredentials(AccountViewModel account, ToolkitRegion region)
        {
            const int maxServiceSpecificCredentials = 2;

            try
            {
                var iamClient = account.CreateServiceClient<AmazonIdentityManagementServiceClient>(region);

                // First, is the user running as an iam user or as root? If the latter, we can't help them
                var username = iamClient.GetUser().User.UserName;
                if (string.IsNullOrEmpty(username))
                {
                    string confirmMsg = "Your profile is using root AWS credentials. AWS CodeCommit requires specific CodeCommit credentials from an IAM user. "
                                            + $"The toolkit can create an IAM user with CodeCommit credentials and associate the credentials with the {account.DisplayName} Toolkit profile."
                                            + Environment.NewLine
                                            + Environment.NewLine
                                            + $"Proceed to try and create an IAM user with credentials and associate with the {account.DisplayName} Toolkit profile?";

                    if (!ToolkitContext.ToolkitHost.Confirm("Auto-create Git Credentials", confirmMsg, MessageBoxButton.YesNo))
                    {
                        return null;
                    }

                    return CreateCodeCommitCredentialsForRoot(iamClient);
                }

                var listCredentialsRequest = new ListServiceSpecificCredentialsRequest
                {
                    ServiceName = codeCommitServiceName,
                    UserName = username
                };
                var listCredentialsResponse = iamClient.ListServiceSpecificCredentials(listCredentialsRequest);

                var credentialsExist = listCredentialsResponse.ServiceSpecificCredentials.Any(ssc => ssc.Status == StatusType.Active);
                if (credentialsExist)
                {
                    LOGGER.InfoFormat("User profile {0} already has service-specific credentials for CodeCommit; user must import credentials", account.DisplayName);
                    return null;
                }

                // IAM limits users to two sets of credentials - inactive credentials count against this limit, so 
                // if we already have two inactive sets, give up
                if (listCredentialsResponse.ServiceSpecificCredentials.Count == maxServiceSpecificCredentials)
                {
                    LOGGER.InfoFormat("User profile {0} already has the maximum amount of service-specific credentials for CodeCommit; user will have to activate and import credentials", account.DisplayName);
                    return null;
                }

                // Account is compatible, so let's see if the user wants us to go ahead
                string msg = "Your account needs Git credentials to be generated to work with AWS CodeCommit. The toolkit can try and create these credentials for you, and download "
                             + $"them for you to save for future use. {Environment.NewLine}{Environment.NewLine}Proceed to try and create credentials?";

                if (!ToolkitContext.ToolkitHost.Confirm("Auto-create Git Credentials", msg, MessageBoxButton.YesNo))
                {
                    return null;
                }

                // Attempt to create a set of credentials and if successful, prompt the user to save them
                var createCredentialRequest = new CreateServiceSpecificCredentialRequest
                {
                    ServiceName = codeCommitServiceName,
                    UserName = username
                };

                var createCredentialsResponse = iamClient.CreateServiceSpecificCredential(createCredentialRequest);
                
                // Seen cases where we've had a 403 error from inside git, as if the creds have
                // not propagated if the user is too quick with the dialog, so force a small delay
                Thread.Sleep(3000);
            
                PromptToSaveGeneratedCredentials(createCredentialsResponse.ServiceSpecificCredential);

                return ServiceSpecificCredentials
                    .FromCredentials(createCredentialsResponse.ServiceSpecificCredential.ServiceUserName,
                                        createCredentialsResponse.ServiceSpecificCredential.ServicePassword);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Exception while probing for CodeCommit credentials in IAM, user must supply manually", ex);
            }

            return null;
        }

        private ServiceSpecificCredentials CreateCodeCommitCredentialsForRoot(IAmazonIdentityManagementService iamClient)
        {
            const string iamUserBaseName = "VSToolkit-CodeCommitUser";

            var listOfUsers = new HashSet<string>();
            var listResponse = new ListUsersResponse();

            do
            {
                listResponse = iamClient.ListUsers(new ListUsersRequest { Marker = listResponse.Marker});
                listOfUsers.AddAll(listResponse.Users.Select(u => u.UserName));
            } while (listResponse.IsTruncated);

            var iamUserName = iamUserBaseName;
            for (var i = 1; listOfUsers.Contains(iamUserName); ++i)
            {
                iamUserName = $"{iamUserBaseName}-{i}";
            }

            iamClient.CreateUser(new CreateUserRequest
            {
                UserName = iamUserName
            });

            iamClient.AttachUserPolicy(new AttachUserPolicyRequest
            {
                UserName = iamUserName,
                PolicyArn = "arn:aws:iam::aws:policy/AWSCodeCommitPowerUser"
            });

            // Attempt to create a set of credentials and if successful, prompt the user to save them
            var createCredentialRequest = new CreateServiceSpecificCredentialRequest
            {
                ServiceName = codeCommitServiceName,
                UserName = iamUserName
            };
            var createCredentialsResponse = iamClient.CreateServiceSpecificCredential(createCredentialRequest);

            // Seen cases where we've had a 403 error from inside git, as if the creds have
            // not propagated if the user is too quick with the dialog, so force a small delay
            Thread.Sleep(3000);

            var msg = $"The IAM user {iamUserName} was created and associated with the AWSCodeCommitPowerUser policy. "
                      + "AWS CodeCommit credentials to enable Git access to your repository have also been created for you.";

            PromptToSaveGeneratedCredentials(createCredentialsResponse.ServiceSpecificCredential, msg);

            return ServiceSpecificCredentials.FromCredentials(createCredentialsResponse.ServiceSpecificCredential.ServiceUserName,
                                                              createCredentialsResponse.ServiceSpecificCredential.ServicePassword);
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
            catch (Exception ex)
            {
                LOGGER.Error("Exception while attempting to find CodeCommit remote url", ex);
            }

            return null;
        }

        private Remote FindCodeCommitRemote(Repository repo)
        {
            return repo.Network?.Remotes.FirstOrDefault(r => r.Url.IndexOf(CodeCommitUrlPrefix, 0, StringComparison.OrdinalIgnoreCase) != -1);
        }

        private string FindCommitRemoteUrl(Repository repo)
        {
            return FindCodeCommitRemote(repo)?.Url;
        }

        private void ExtractRepoNameAndRegion(string codeCommitRemoteUrl, out string repoName, out ToolkitRegion region)
        {
            region = null;
            ExtractRepoNameAndRegion(codeCommitRemoteUrl, out repoName, out string regionId);

            if (!string.IsNullOrWhiteSpace(regionId))
            {
                region = ToolkitContext.RegionProvider.GetRegion(regionId);
            }
        }

        private void ExtractRepoNameAndRegion(string codeCommitRemoteUrl, out string repoName, out string region)
        {
            // possibly fragile, but expecting host to be 'git-codecommit.REGION.suffix/.../reponame
            var serviceNameStart = codeCommitRemoteUrl.IndexOf(CodeCommitUrlPrefix, 0, StringComparison.OrdinalIgnoreCase);

            if (serviceNameStart == -1)
            {
                throw new ArgumentException("Not a CodeCommit remote url");
            }

            var regionStartPos = codeCommitRemoteUrl.IndexOf(".", serviceNameStart, StringComparison.OrdinalIgnoreCase) + 1;
            var regionEndPos = codeCommitRemoteUrl.IndexOf(".", regionStartPos, StringComparison.OrdinalIgnoreCase);

            region = codeCommitRemoteUrl.Substring(regionStartPos, regionEndPos - regionStartPos);

            var lastSlashPos = codeCommitRemoteUrl.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
            repoName = codeCommitRemoteUrl.Substring(lastSlashPos + 1);
        }

        private Dictionary<string, Dictionary<string, List<string>>> GroupLocalRepositoriesByRegion(IEnumerable<string> pathsToRepositories)
        {
            // Associate the path with a repo name using a dictionary, so we get a fast lookup
            // when we're post-processing the batch metadata query which yields repo metadata by name.
            // Why List<string>? Because the user may have cloned the same repo into different paths, but
            // we only need the repo name once we region.
            var repositoryNameAndPathByRegion = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (var path in pathsToRepositories)
            {
                // Find the remote for CodeCommit, and from that we can infer the region and the
                // actual repo name. Trying to auto-discover this info without needing to perist
                // data about the repos we've seen
                if (!Repository.IsValid(path))
                {
                    continue;
                }

                var repo = new Repository(path);
                var codeCommitRemoteUrl = FindCommitRemoteUrl(repo);
                if (codeCommitRemoteUrl == null)
                {
                    continue;
                }

                ExtractRepoNameAndRegion(codeCommitRemoteUrl, out string repoName, out string region);

                if (repositoryNameAndPathByRegion.ContainsKey(region))
                {
                    var processedRepos = repositoryNameAndPathByRegion[region];
                    if (!processedRepos.ContainsKey(repoName))
                    {
                        repositoryNameAndPathByRegion[region].Add(repoName, new List<string> { path });
                    }
                    else
                    {
                        var l = processedRepos[repoName];
                        l.Add(path);
                    }
                }
                else
                {
                    var names = new Dictionary<string, List<string>> { { repoName, new List<string> {path} } };
                    repositoryNameAndPathByRegion.Add(region, names);
                }
            }

            return repositoryNameAndPathByRegion;
        }

        private void ThrowOnInvalidRepo(string repoPath)
        {
            if (!(Directory.Exists(repoPath) && Repository.IsValid(repoPath)))
            {
                throw new Exception($"Repository path {repoPath} does not exist.");
            }
        }

        private ToolkitRegion GetFallbackRegion()
        {
            if (_fallbackRegion == null)
            {
                _fallbackRegion = ToolkitContext?.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName);
            }

            return _fallbackRegion;
        }
    }
}
