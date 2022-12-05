using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;

using EnvDTE;

using log4net;

using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.VisualStudio.Shell;
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

            CloneCommand = new RelayCommand(CanOperateOnActiveConnection, param => OnClone());
            CreateCommand = new RelayCommand(CanOperateOnActiveConnection, param => OnCreate());
            SignoutCommand = new CommandHandler(OnSignout, true);
            RefreshConnection = new RelayCommand(CanRefreshConnection, param => OnRefreshConnection());

            Title = "AWS CodeCommit";
        }

        /// <summary>
        /// Monitors for changes in the active connection and wires up to receive
        /// repository list change events when a connection is established.
        /// </summary>
        private void OnTeamExplorerBindingChanged(TeamExplorerConnection oldConnection, TeamExplorerConnection newConnection)
        {
            LOGGER.Debug("ConnectionSectionViewModel OnTeamExplorerBindingChanged");
            if (oldConnection != null)
            {
                oldConnection.PropertyChanged -= ActiveConnectionOnPropertyChanged;
            }

            if (newConnection != null)
            {
                newConnection.PropertyChanged += ActiveConnectionOnPropertyChanged;
            }

            RaisePropertyChanged(nameof(Repositories));
            RaisePropertyChanged(nameof(SignoutLabel));
        }

        private void ActiveConnectionOnPropertyChanged(object o, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e.PropertyName);

            if (e.PropertyName == nameof(TeamExplorerConnection.AwsConnectionState))
            {
                RaisePropertyChanged(nameof(IsAccountValid));
                RaisePropertyChanged(nameof(AccountValidationMessage));

                // Trigger a refresh of the enabled state of commands like Clone
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    CommandManager.InvalidateRequerySuggested();
                });
            }
        }

        public string SignoutLabel =>
            TeamExplorerConnection.ActiveConnection == null
                ? "Sign out"
                : $"Sign out {TeamExplorerConnection.ActiveConnection.Account.DisplayName}";

        public ICommand SignoutCommand { get; }

        public ICommand CloneCommand { get; }

        public ICommand CreateCommand { get; }

        public ICommand RefreshConnection { get; }

        public ObservableCollection<ICodeCommitRepository> Repositories => TeamExplorerConnection.ActiveConnection?.Repositories;

        public ICodeCommitRepository SelectedRepository { get; set; }

        // Enable access to the account here, so that if we ever need to support
        // multiple connections each panel can have its own without a larger refactor
        public AccountViewModel Account => TeamExplorerConnection.ActiveConnection?.Account;

        public bool IsAccountValid => TeamExplorerConnection.ActiveConnection?.IsAccountValid ?? false;

        public string AccountValidationMessage => TeamExplorerConnection.ActiveConnection?.AccountValidationMessage ?? string.Empty;

        private bool CanOperateOnActiveConnection(object o)
        {
            if (TeamExplorerConnection.ActiveConnection == null)
            {
                return false;
            }

            return IsAccountValid &&
                   !TeamExplorerConnection.ActiveConnection.IsValidatingAccount;
        }

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

        private bool CanRefreshConnection(object param)
        {
            return
                !IsAccountValid
                && (TeamExplorerConnection.ActiveConnection?.AwsConnectionState.IsTerminal ?? false);
        }

        private void OnRefreshConnection()
        {
            TeamExplorerConnection.ActiveConnection?.RevalidateConnection();
        }

        public void OpenRepository()
        {
            LOGGER.Debug("ConnectionSectionViewModel OpenRepository");

            // There is no procedure to bind to a repo from within Team Explorer, so adopt
            // GitHub's approach of opening a transient solution in the repo, that we then
            // discard
            const string TempSolutionName = "~$AWSToolkitVSTemp$~";

            // TODO: If currently open repo == the one we clicked, do nothing

            var dte = ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<DTE>();
            if (dte == null)
            {
                LOGGER.Info("Unable to get DTE service from shell, abandoning repository open.");
                return;
            }

            var repoDir = SelectedRepository.LocalFolder;
            if (!Directory.Exists(repoDir))
            {
                LOGGER.Info("Folder for the selected repo does not exist, abandoning repository open.");
                return;
            }

            var solutionCreated = false;
            try
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    dte.Solution.Create(repoDir, TempSolutionName);
                    solutionCreated = true;
                    dte.Solution.Close(false); // Don't create a .sln file when we close.
                });
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error opening repository", ex);
            }
            finally
            {
                // Clean up any generated artifacts as a reslt of our temp solution open
                var vsTempPath = Path.Combine(repoDir, ".vs", TempSolutionName);
                try
                {
                    // Clean up the dummy solution's subdirectory inside `.vs`.
                    if (Directory.Exists(vsTempPath))
                    {
                        Directory.Delete(vsTempPath, true);
                    }
                }
                catch (Exception ex)
                {
                    LOGGER.Error("Exception attempting to clean up temp solution files", ex);
                }
            }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (solutionCreated)
                {
                    var teamExplorerService = await ToolkitFactory.Instance.ShellProvider.QueryShellProviderServiceAsync<ITeamExplorer>();
                    teamExplorerService?.NavigateToPage(new Guid(TeamExplorerPageIds.Home), null);
                }
                else
                {
                    var solutionService = await ToolkitFactory.Instance.ShellProvider.QueryShellProviderServiceAsync<IVsSolution>();
                    solutionService?.OpenSolutionViaDlg(SelectedRepository.LocalFolder, 1);
                }
            });
        }
    }
}
