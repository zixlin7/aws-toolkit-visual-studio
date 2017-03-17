using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls;
using log4net;
using LibGit2Sharp;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    /// <summary>
    /// Controller for cloning a repository. User can select from a list
    /// or the controller can be run headless by passing in details of the
    /// repo to clone and the local folder.
    /// </summary>
    public class CloneRepositoryController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(CloneRepositoryController));

        /// <summary>
        /// Constructs a controller that will display a dialog for repository selection
        /// and perform the clone on user selection.
        /// </summary>
        /// <param name="account"></param>
        public CloneRepositoryController(AccountViewModel account)
        {
            Model = new CloneRepositoryModel
            {
                Account = account
            };
        }

        /// <summary>
        /// Constructs a controller to clone a repository in headless manner.
        /// </summary>
        /// <param name="serviceCredentials"></param>
        /// <param name="repositoryUrl"></param>
        /// <param name="localFolder"></param>
        public CloneRepositoryController(ServiceSpecificCredentials serviceCredentials, string repositoryUrl, string localFolder)
        {
            Model = new CloneRepositoryModel
            {
                ServiceCredentials = serviceCredentials,
                RepositoryUrl = repositoryUrl,
                LocalFolder = localFolder
            };
        }

        public CloneRepositoryModel Model { get; }

        public CloneRepositorySelectorControl View { get; private set; }

        public ActionResults Execute()
        {
            if (!string.IsNullOrEmpty(Model.RepositoryUrl))
                return CloneRepository();

            // display selector dialog and guide the user through the process
            View = new CloneRepositorySelectorControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(View))
            {
                // if the account does not have service-specific credentials, ask for them
                var svcCredentials = ServiceSpecificCredentialStoreManager.Instance.GetCredentialsForService(Model.Account.SettingsUniqueKey, "codecommit");
                if (svcCredentials == null)
                {
                    var registerCredentialsController = new RegisterServiceCredentialsController(Model.Account);
                    if (!registerCredentialsController.Execute().Success)
                        return new ActionResults().WithSuccess(false);

                    svcCredentials = registerCredentialsController.Credentials;
                }

                // for now, append the repo name onto the selected path - we'll want to show
                // this in the dialog eventually
                var finalPathComponent = Path.GetFileName(Model.LocalFolder);
                if (!finalPathComponent.Equals(Model.SelectedRepository.Name, StringComparison.OrdinalIgnoreCase))
                {
                    Model.LocalFolder = Path.Combine(Model.LocalFolder, Model.SelectedRepository.Name);
                }

                Model.ServiceCredentials = svcCredentials;
                Model.RepositoryUrl = Model.SelectedRepository.RepositoryMetadata.CloneUrlHttp;
                return CloneRepository();
            }

            return new ActionResults().WithSuccess(false);   
        }

        /// <summary>
        /// Performs the clone operation once the url, local folder and credentials to use are known.
        /// </summary>
        private ActionResults CloneRepository()
        {
            try
            {
                var co = new CloneOptions
                {
                    CredentialsProvider =
                        (url, user, cred) =>
                            new UsernamePasswordCredentials
                            {
                                Username = Model.ServiceCredentials.Username,
                                Password = Model.ServiceCredentials.Password
                            }
                };

                Repository.Clone(Model.RepositoryUrl, Model.LocalFolder, co);
                return new ActionResults().WithSuccess(true);
            }
            catch (Exception e)
            {
                // todo: need to display in the UI too
                var msg = "Libgit2sharp exception while cloning repository: " + e.Message;
                LOGGER.Error(msg, e);
                return new ActionResults().WithSuccess(false);
            }
        }

    }
}
