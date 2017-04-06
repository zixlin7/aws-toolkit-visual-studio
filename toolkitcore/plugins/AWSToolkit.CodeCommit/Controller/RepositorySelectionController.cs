using System;
using System.IO;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CodeCommit.Controls;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;
using log4net;
using LibGit2Sharp;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    /// <summary>
    /// Controller for prompting the user to select a repository for cloning.
    /// </summary>
    public class RepositorySelectionController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(RepositorySelectionController));

        /// <summary>
        /// Constructs a controller that will display a dialog for repository selection.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="initialRegion">The initial region binding for the dialog</param>
        /// <param name="defaultCloneFolderRoot">The system default folder for cloned repos, discovered from the registry or a fallback default</param>
        public RepositorySelectionController(AccountViewModel account, RegionEndPointsManager.RegionEndPoints initialRegion, string defaultCloneFolderRoot)
        {
            Model = new RepositorySelectionModel
            {
                Account = account,
                SelectedRegion = initialRegion ?? RegionEndPointsManager.Instance.GetRegion("us-east-1"),
                LocalFolder = defaultCloneFolderRoot
            };
        }

        public RepositorySelectionModel Model { get; }

        public RepositorySelectionControl View { get; private set; }

        public ActionResults Execute()
        {
            View = new RepositorySelectionControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(View))
            {
                /*
                // if the account does not have service-specific credentials, ask for them
                var svcCredentials 
                    = ServiceSpecificCredentialStoreManager
                            .Instance
                            .GetCredentialsForService(Model.Account.SettingsUniqueKey,
                                                      CodeCommitActivator.CodeCommitServiceCredentialsName);
                if (svcCredentials == null)
                {
                    var registerCredentialsController = new RegisterServiceCredentialsController(Model.Account);
                    if (!registerCredentialsController.Execute().Success)
                        return new ActionResults().WithSuccess(false);

                    svcCredentials = registerCredentialsController.Credentials;
                }
                Model.ServiceCredentials = svcCredentials;
                */

                // for now, append the repo name onto the selected path - we'll want to show
                // this in the dialog eventually
                var finalPathComponent = Path.GetFileName(Model.LocalFolder);
                if (!finalPathComponent.Equals(Model.SelectedRepository.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Model.LocalFolder = Path.Combine(Model.LocalFolder, Model.SelectedRepository.Name);
                }

                Model.RepositoryUrl = Model.SelectedRepository.RepositoryMetadata.CloneUrlHttp;
                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);   
        }
    }
}
