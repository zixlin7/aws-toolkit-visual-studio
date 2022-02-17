using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controls;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Manages the connection flow to CodeCommit. This involves selecting, or creating,
    /// an AWS credential profile. The selected profile is then registered as the active
    /// TeamExplorer connection with the connection manager.
    /// </summary>
    public class ConnectController
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectController));

        private ConnectControl _selectionControl;

        public ActionResults Execute()
        {
            if (ToolkitFactory.Instance?.RootViewModel == null)
            {
                // The Toolkit (extension) has not been loaded and initialized yet.
                // Prevent a null access exception
                Logger.Error("Tried to connect to CodeCommit, but the Toolkit has not been loaded yet.");
                return new ActionResults().WithSuccess(false);
            }

            var accounts = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts;
            // if the user has only one profile, we can just proceed
            if (accounts.Count == 1)
            {
                GitUtilities.RecordCodeCommitSetCredentialsMetric(true);
                SelectedAccount = accounts.First();
                return new ActionResults().WithSuccess(true);
            }

            var results = accounts.Any() ? SelectFromExistingProfiles() : CreateNewProfile();
            
            GitUtilities.RecordCodeCommitSetCredentialsMetric(results.Success);

            return results;
        }

        public AccountViewModel SelectedAccount { get; private set; }

        /// <summary>
        /// Called from the main package in response to the user selecting
        /// CodeCommit under the Manage Connections dropdown
        /// </summary>
        public static void  OpenConnection()
        {
            if (TeamExplorerConnection.ActiveConnection != null)
                TeamExplorerConnection.ActiveConnection.Signout();

            var controller = new ConnectController();
            var results = controller.Execute();
            if (results.Success)
            {
                TeamExplorerConnection.Signin(controller.SelectedAccount);
            }
        }

        private ActionResults SelectFromExistingProfiles()
        {
            _selectionControl = new ConnectControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(_selectionControl))
            {
                SelectedAccount = _selectionControl.SelectedAccount;
                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);
        }

        private ActionResults CreateNewProfile()
        {
            if (ToolkitFactory.Instance?.ToolkitContext == null)
            {
                return new ActionResults().WithSuccess(false);
            }

            var registrationController = new RegisterAccountController(ToolkitFactory.Instance.ToolkitContext);
            var results = registrationController.Execute();
            if (results.Success)
            {
                ToolkitFactory.Instance.RootViewModel.Refresh();
                SelectedAccount = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts.FirstOrDefault();
                return results;
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
