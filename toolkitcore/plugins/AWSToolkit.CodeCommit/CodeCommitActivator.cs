using System;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.Util;
using log4net;
using LibGit2Sharp;

namespace Amazon.AWSToolkit.CodeCommit
{
    public class CodeCommitActivator : AbstractPluginActivator, IAWSCodeCommit
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(CodeCommitActivator));
        private const string CodeCommitServiceCredentialsName = "codecommit";

        public override string PluginName
        {
            get { return "CodeCommit"; }
        }

        public override void RegisterMetaNodes()
        {
            
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSCodeCommit))
                return this;

            return null;
        }

        #region IAWSCodeCommit Members

        public string ServiceSpecificCredentialsStorageName
        {
            get { return CodeCommitServiceCredentialsName; }
        }

        public void AssociateCredentialsWithProfile(string profileArtifactsId, string userName, string password)
        {
            ServiceSpecificCredentialStoreManager
                .Instance
                .SaveCredentialsForService(profileArtifactsId,
                                           CodeCommitServiceCredentialsName,
                                           userName,
                                           password);
        }

        public ServiceSpecificCredentials CredentialsForProfile(string profileArtifactsId)
        {
            return 
                ServiceSpecificCredentialStoreManager
                    .Instance
                    .GetCredentialsForService(profileArtifactsId, CodeCommitServiceCredentialsName);
        }

        public bool CloneRepository(AccountViewModel account)
        {
            var controller = new CloneRepositoryController(account);
            return controller.Execute().Success;
        }

        public bool CloneRepository(ServiceSpecificCredentials credentials, string cloneUrlHttp, string  localFolder)
        {
            var controller = new CloneRepositoryController(credentials, cloneUrlHttp, localFolder);
            return controller.Execute().Success;
        }

        #endregion
    }
}
