using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers
{
    /// <summary>
    /// Manages the connection flow to CodeCommit. This involves selecting, or creating,
    /// an AWS credential profile. The selected profile is then registered as the active
    /// TeamExplorer connection with the connection manager.
    /// </summary>
    public class ConnectController
    {
        private ConnectControl _selectionControl;

        public ActionResults Execute()
        {
            var accounts = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts;
            // if the user has only one profile, we can just proceed
            if (accounts.Count == 1)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.CodeCommitConnectStatus, ToolkitEvent.COMMON_STATUS_SUCCESS);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                SelectedAccount = accounts.First();
                return new ActionResults().WithSuccess(true);
            }


            var results = accounts.Any() ? SelectFromExistingProfiles() : CreateNewProfile();

            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.CodeCommitConnectStatus, results.Success ? ToolkitEvent.COMMON_STATUS_SUCCESS : ToolkitEvent.COMMON_STATUS_FAILURE);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
            }

            return results;
        }

        public AccountViewModel SelectedAccount { get; private set; }

        /// <summary>
        /// Called from the main package in response to the user selecting
        /// CodeCommit under the Manage Connections dropdown
        /// </summary>
        public static void OpenConnection()
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
            var registrationController = new RegisterAccountController();
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
