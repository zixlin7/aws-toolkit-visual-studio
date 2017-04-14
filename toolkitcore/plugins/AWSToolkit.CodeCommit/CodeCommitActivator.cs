using System;
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
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.CodeCommit
{
    public class CodeCommitActivator : AbstractPluginActivator, IAWSCodeCommit
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitActivator));

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

        public string ServiceSpecificCredentialsStorageName => ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName;

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
                    .GetCredentialsForService(profileArtifactsId, ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName);
        }

        public ServiceSpecificCredentials ObtainGitCredentials(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            var svcCredentials
                = ServiceSpecificCredentialStoreManager
                    .Instance
                    .GetCredentialsForService(account.SettingsUniqueKey, ServiceSpecificCredentialStoreManager.CodeCommitServiceCredentialsName);

            if (svcCredentials != null)
                return svcCredentials;

            // nothing local, so first see if we can create credentials for the user if they
            // haven't already done so
            svcCredentials = ProbeIamForServiceSpecificCredentials(account, region);
            if (svcCredentials != null)
            {
                AssociateCredentialsWithProfile(account.SettingsUniqueKey, svcCredentials.Username, svcCredentials.Password);
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

        public IAWSToolkitGitServices ToolkitGitServices => new AWSToolkitGitServices(this);

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
                    LOGGER.InfoFormat("User profile {0} contains root credentials; cannot be used to create service-specific credentials.", account.DisplayName);
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
                    LOGGER.InfoFormat("User profile {0} already has service-specific credentials for CodeCommit; user must import credentials", account.DisplayName);
                    return null;
                }

                // IAM limits users to two sets of credentials - inactive credentials count against this limit, so 
                // if we already have two inactive sets, give up
                if (listCredentialsReponse.ServiceSpecificCredentials.Count == maxServiceSpecificCredentials)
                {
                    LOGGER.InfoFormat("User profile {0} already has the maximum amount of service-specific credentials for CodeCommit; user will have to activate and import credentials", account.DisplayName);
                    return null;
                }

                // account is compatible, so let's see if the user wants us to go ahead
                const string msg = "Your account needs Git credentials to be generated to work with AWS CodeCommit. "
                                   +
                                   "The toolkit can try and create these credentials for you, and download them for you to save for future use. "
                                   + "\r\n"
                                   + "\r\n"
                                   + "Proceed to try and create credentials?";

                if (!ToolkitFactory.Instance.ShellProvider.Confirm("Auto-create Git Credentials", msg, MessageBoxButton.YesNo))
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

    }
}
