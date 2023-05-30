using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controls;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using log4net;
using Amazon.AWSToolkit.Exceptions;

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
            var results = Connect();
            CodeCommitTelemetryUtils.RecordCodeCommitSetCredentialsMetric(results);
            return results;
        }

        private ActionResults Connect()
        {
            if (ToolkitFactory.Instance?.RootViewModel == null)
            {
                // The Toolkit (extension) has not been loaded and initialized yet.
                // Prevent a null access exception
                Logger.Error("Tried to connect to CodeCommit, but the Toolkit has not been loaded yet.");
                return ActionResults.CreateFailed(new ToolkitException("Unable to find CodeCommit data", ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            // If the user has only one profile, we can just proceed
            var accounts = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts;
            if (accounts.Count == 1)
            {
                SelectedAccount = accounts.First();
                return new ActionResults().WithSuccess(true);
            }

            var results = accounts.Any() ? SelectFromExistingProfiles() : CreateNewProfile();
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
            {
                TeamExplorerConnection.ActiveConnection.Signout();
            }

            var controller = new ConnectController();
            var results = controller.Execute();
            if (results.Success)
            {
                TeamExplorerConnection.Signin(controller.SelectedAccount);
            }
        }

        private ActionResults SelectFromExistingProfiles()
        {
            _selectionControl = new ConnectControl();
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(_selectionControl))
            {
                SelectedAccount = _selectionControl.SelectedAccount;
                return new ActionResults().WithSuccess(true);
            }

            return ActionResults.CreateCancelled();
        }

        private ActionResults CreateNewProfile()
        {
            if (ToolkitFactory.Instance?.ToolkitContext == null)
            {
                return ActionResults.CreateFailed(new ToolkitException("Unable to find CodeCommit data", ToolkitException.CommonErrorCode.InternalMissingServiceState));
            }

            var registrationController = new LegacyRegisterAccountController(ToolkitFactory.Instance.ToolkitContext);
            var results = registrationController.Execute(MetricSources.CodeCommitMetricSource.ConnectPanel);

            if (results.Success)
            {
                ToolkitFactory.Instance.RootViewModel.Refresh();
                SelectedAccount = ToolkitFactory.Instance.RootViewModel.RegisteredAccounts.FirstOrDefault();
            }

            return results;
        }
    }
}
