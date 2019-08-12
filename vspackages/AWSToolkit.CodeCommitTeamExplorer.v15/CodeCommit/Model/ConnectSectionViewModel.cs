using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using EnvDTE;
using log4net;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Model
{
    public class ConnectSectionViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(ConnectSectionViewModel));

        public ConnectSectionViewModel()
        {
            TeamExplorerConnection.OnTeamExplorerBindingChanged += OnTeamExplorerBindingChanged;
            if (TeamExplorerConnection.ActiveConnection != null)
            {
                TeamExplorerConnection.ActiveConnection.PropertyChanged += ActiveConnectionOnPropertyChanged;
            }

            _cloneCommand = new CommandHandler(OnClone, true);
            _createCommand = new CommandHandler(OnCreate, true);
            _signoutCommand = new CommandHandler(OnSignout, true);
        }

        /// <summary>
        /// Monitors for changes in the active connection and wires up to receive
        /// repository list change events when a connection is established.
        /// </summary>
        /// <param name="connection"></param>
        private void OnTeamExplorerBindingChanged(TeamExplorerConnection connection)
        {
            LOGGER.InfoFormat("ConnectionSectionViewModel OnTeamExplorerBindingChanged");
            if (connection != null)
            {
                connection.PropertyChanged += ActiveConnectionOnPropertyChanged;
            }

            RaisePropertyChanged("Repositories");
            RaisePropertyChanged("SignoutLabel");
        }

        private void ActiveConnectionOnPropertyChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            RaisePropertyChanged(propertyChangedEventArgs.PropertyName);
        }

        public string SignoutLabel =>
            TeamExplorerConnection.ActiveConnection == null
                ? "Sign out"
                : string.Concat("Sign out ", TeamExplorerConnection.ActiveConnection.Account.DisplayName);

        private readonly CommandHandler _signoutCommand;

        public ICommand SignoutCommand => _signoutCommand;

        private readonly CommandHandler _cloneCommand;

        public ICommand CloneCommand => _cloneCommand;

        private readonly CommandHandler _createCommand;

        public ICommand CreateCommand => _createCommand;

        public ObservableCollection<ICodeCommitRepository> Repositories =>
            TeamExplorerConnection.ActiveConnection != null
                ? TeamExplorerConnection.ActiveConnection.Repositories
                : null;

        public ICodeCommitRepository SelectedRepository { get; set; }

        // enable access to the account here, so that if we ever need to support
        // multiple connections each panel can have its own without a larger refactor
        public AccountViewModel Account => TeamExplorerConnection.ActiveConnection?.Account;

        private void OnClone()
        {
            new CloneRepositoryController(ServiceProvider).Execute();
        }

        private void OnCreate()
        {
            new CreateRepositoryController(ServiceProvider).Execute();
        }

        private void OnSignout()
        {
            TeamExplorerConnection.ActiveConnection.Signout();
        }

        public void OpenRepository()
        {
            LOGGER.InfoFormat("ConnectionSectionViewModel OpenRepository");
            // there is no procedure to bind to a repo from within Team Explorer, so adopt
            // GitHub's approach of opening a transient solution in the repo, that we then
            // discard
            const string TempSolutionName = "~$AWSToolkitVSTemp$~";

            // todo: if currently open repo == the one we clicked, do nothing

            var dte = ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<DTE>();
            if (dte == null)
            {
                LOGGER.Info("Unable to get DTE service from shell, abandoning repository open.");
                return;
            }

            var repoDir = SelectedRepository.LocalFolder;
            if (!Directory.Exists(repoDir))
            {
                LOGGER.Info("Folder for the selected repo does not exist, abanding repository open.");
                return;
            }

            var solutionCreated = false;
            try
            {
                dte.Solution.Create(repoDir, TempSolutionName);
                solutionCreated = true;

                dte.Solution.Close(false); // Don't create a .sln file when we close.
            }
            catch (Exception e)
            {
                LOGGER.Error("Error opening repository", e);
            }
            finally
            {
                // clean up any generated artifacts as a reslt of our temp solution open
                var vsTempPath = Path.Combine(repoDir, ".vs", TempSolutionName);
                try
                {
                    // Clean up the dummy solution's subdirectory inside `.vs`.
                    if (Directory.Exists(vsTempPath))
                    {
                        Directory.Delete(vsTempPath, true);
                    }
                }
                catch (Exception e)
                {
                    LOGGER.Error("Exception attempting to clean up temp solution files", e);
                }
            }

            Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (solutionCreated)
                {
                    var teamExplorerService =
                        ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<ITeamExplorer>();
                    teamExplorerService?.NavigateToPage(new Guid(TeamExplorerPageIds.Home), null);
                }
                else
                {
                    var solutionService = ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IVsSolution>();
                    solutionService?.OpenSolutionViaDlg(SelectedRepository.LocalFolder, 1);
                }
            });
        }
    }
}
