using System;
using Amazon.AWSToolkit.Account;
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
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.CodeCommit
{
    public class CodeCommitActivator : AbstractPluginActivator, IAWSCodeCommit
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitActivator));

        public override string PluginName => "CodeCommit";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new CodeCommitRootViewMetaNode();
            var repositoryMetaNode = new CodeCommitRepositoryViewMetaNode();

            rootMetaNode.Children.Add(repositoryMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
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

        public IRepository SelectRepositoryToClone(AccountViewModel account, RegionEndPointsManager.RegionEndPoints initialRegion, string defaultCloneFolderRoot)
        {
            var controller = new RepositorySelectionController(account, initialRegion, defaultCloneFolderRoot);
            if (!controller.Execute().Success)
                return null;

            return new RepositoryWrapper(controller.Model.SelectedRepository, controller.Model.LocalFolder);
        }

        public string PromptToSaveGeneratedCredentials(ServiceSpecificCredential generatedCredentials)
        {
            var controller = new SaveServiceSpecificCredentialsController(generatedCredentials);
            return controller.Execute().Success ? controller.SelectedFilename : null;
        }

        public IAWSToolkitGitServices ToolkitGitServices => new AWSToolkitGitServices(this);

        #endregion
    }
}
