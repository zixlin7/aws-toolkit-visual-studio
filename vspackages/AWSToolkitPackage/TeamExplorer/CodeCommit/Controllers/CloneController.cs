using System;
using System.IO;
using System.Linq;
using Microsoft.Win32;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.Model;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Sequences the process of cloning a repository from CodeCommit inside Team Explorer, 
    /// ensuring the user has valid service-specific credentials.
    /// </summary>
    internal class CloneController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CloneController));
        private const int MaxServiceSpecificCredentials = 2;

        private IAWSCodeCommit CodeCommitPlugin { get; set; }
        private AccountViewModel Account { get; set; }
        private RegionEndPointsManager.RegionEndPoints Region { get; set; }

        /// <summary>
        /// Interactively clones a repository. The user is first asked to select the repo
        /// to clone and the local folder location. Once this is established we determine if
        /// service specific credentials are available to be set into the OS credential store
        /// for Team Explorer/git to work with:
        /// 1. If credentials are available locally for the selected user a/c, we use them silently
        /// 2. If no credentials are found locally, we determine if a set has been created in IAM
        /// 2a. If no credentials exist in IAM, we attempt to create some and prompt the user to
        ///     save the downloaded credentials if successful.
        /// 2b. If the attempt to create fails, we direct the user to supply credentials manually.
        /// </summary>
        /// <returns></returns>
        public void Execute()
        {
            CodeCommitPlugin = ToolkitFactory.Instance.QueryPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
            if (CodeCommitPlugin == null)
            {
                LOGGER.Error("Called to clone repository but CodeCommit plugin not loaded, cannot display repository list selector");
                return;
            }

            // by now we'll have made sure the user has a profile set up, so default account and region
            // bindings are always available
            Account = ConnectionsManager.Instance.TeamExplorerAccount;
            Region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;

            var selectedRepository = CodeCommitPlugin.SelectRepositoryToClone(Account, Region, GetLocalClonePathFromGitProvider());
            if (selectedRepository == null)
                return;

            var gitCredentials = ObtainGitCredentials();
            if (gitCredentials == null)
                return;

            // delegate the actual clone operation via an intermediary; this allows us to use either
            // Team Explorer or CodeCommit to do the actual clone operation.
            var gitServices = ToolkitFactory
                                  .Instance
                                  .ShellProvider
                                  .QueryShellProviderService<IAWSToolkitGitServices>() ?? ToolkitFactory
                                  .Instance
                                  .QueryPluginService(typeof(IAWSToolkitGitServices)) as IAWSToolkitGitServices;
            gitServices?.Clone(selectedRepository.RepositoryUrl, selectedRepository.LocalFolder, gitCredentials);
        }

        /// <summary>
        /// Looks for service specific credentials to allow https access to git in CodeCommit.
        /// If no credentials are available locally we will attempt to create a set, and if that
        /// fails (or credentials exist but are not local) we will prompt the user to supply them.
        /// </summary>
        /// <returns></returns>
        public ServiceSpecificCredentials ObtainGitCredentials()
        {
            // if the account doesn't have service-specific credentials for CodeCommit, now would be
            // a good time to get and store them
            var svcCredentials
                = ServiceSpecificCredentialStoreManager
                    .Instance
                    .GetCredentialsForService(Account.SettingsUniqueKey, ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName);

            if (svcCredentials != null)
                return svcCredentials;

            // nothing local, so first see if we can create credentials for the user if they
            // haven't already done so
            svcCredentials = ProbeIamForServiceSpecificCredentials();
            if (svcCredentials != null)
            {
                RegisterServiceCredentialsModel.PersistCredentials(svcCredentials, Account.SettingsUniqueKey);
                return svcCredentials;
            }

            // can't autocreate due to use of root account, or no permissions, or they exist so final attempt 
            // is to get the user to perform the steps necessary to get credentials
            var registerCredentialsController = new RegisterServiceCredentialsController(Account);
            return !registerCredentialsController.Execute().Success ? null : registerCredentialsController.Credentials;
        }

        /// <summary>
        /// If the user has no credentials for codecommit, then try and create some and prompt them to
        /// save the downloaded csv file. Note that that auto-create can fail if the user is using root 
        /// credentials or does not have the necessary permissions.
        /// </summary>
        /// <returns></returns>
        public ServiceSpecificCredentials ProbeIamForServiceSpecificCredentials()
        {
            const string iamEndpointsName = "IAM";
            const string codeCommitServiceName = "codecommit.amazonaws.com";

            try
            {
                var iamConfig = new AmazonIdentityManagementServiceConfig
                {
                    ServiceURL = Region.GetEndpoint(iamEndpointsName).Url
                };
                var iamClient = new AmazonIdentityManagementServiceClient(Account.Credentials, iamConfig);

                // First, is the user running as an iam user or as root? If the latter, we can't help them
                var getUserResponse = iamClient.GetUser();
                if (string.IsNullOrEmpty(getUserResponse.User.UserName))
                {
                    LOGGER.InfoFormat("User profile {0} contains root credentials; cannot be used to create service-specific credentials.", Account.DisplayName);
                    return null;
                }

                var listCredentialsRequest = new ListServiceSpecificCredentialsRequest
                {
                    ServiceName = codeCommitServiceName,
                    UserName = getUserResponse.User.UserName
                };
                var listCredentialsReponse = iamClient.ListServiceSpecificCredentials(listCredentialsRequest);
                var credentialsExist = listCredentialsReponse.ServiceSpecificCredentials.Any(ssc => ssc.Status == StatusType.Active);
                if (credentialsExist)
                {
                    LOGGER.InfoFormat("User profile {0} already has service-specific credentials for CodeCommit; user must import credentials", Account.DisplayName);
                    return null;
                }

                // IAM limits users to two sets of credentials - inactive credentials count against this limit, so 
                // if we already have two inactive sets, give up
                if (listCredentialsReponse.ServiceSpecificCredentials.Count == MaxServiceSpecificCredentials)
                {
                    LOGGER.InfoFormat("User profile {0} already has the maximum amount of service-specific credentials for CodeCommit; user will have to activate and import credentials", Account.DisplayName);
                    return null;
                }

                // attempt to create a set of credentials and if successful, prompt the user to save them
                var createCredentialRequest = new CreateServiceSpecificCredentialRequest
                {
                    ServiceName = codeCommitServiceName,
                    UserName = getUserResponse.User.UserName
                };
                var createCredentialsResponse = iamClient.CreateServiceSpecificCredential(createCredentialRequest);
                var filename = CodeCommitPlugin.PromptToSaveGeneratedCredentials(createCredentialsResponse.ServiceSpecificCredential);

                return ServiceSpecificCredentials.FromCsvFile(filename);
            }
            catch (Exception e)
            {
                LOGGER.Error("Exception while probing for CodeCommit credentials in IAM, user must supply manually", e);
            }

            return null;
        }

        /// <summary>
        /// Try and determine the user-preferred local clone path, falling back to the same
        /// default as github if necessary.
        /// </summary>
        /// <returns></returns>
        public string GetLocalClonePathFromGitProvider()
        {
            var clonePath = string.Empty;

            try
            {
#if VS2017_OR_LATER
                const string TEGitKey = @"SOFTWARE\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";
#else
                const string TEGitKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\TeamFoundation\GitSourceControl";
#endif

                using (var key = Registry.CurrentUser.OpenSubKey(TEGitKey + "\\General", true))
                {
                    clonePath = (string)key?.GetValue("DefaultRepositoryPath", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
                }
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Error loading the default cloning path from the registry '{0}'", e);
            }

            if (string.IsNullOrEmpty(clonePath))
            {
                clonePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "Repos");
            }

            return clonePath;
        }
    }
}
