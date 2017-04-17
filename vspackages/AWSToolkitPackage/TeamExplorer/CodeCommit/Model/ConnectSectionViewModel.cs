﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
using EnvDTE;
using log4net;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model
{
    public class ConnectSectionViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(ConnectSectionViewModel));

        public ConnectSectionViewModel()
        {
            TeamExplorerConnection.OnTeamExplorerBindingChanged += OnTeamExplorerBindingChanged;

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
            if (connection != null)
            {
                connection.PropertyChanged += (sender, args) =>
                {
                    RaisePropertyChanged(args.PropertyName);
                };
            }

            RaisePropertyChanged("Repositories");
            RaisePropertyChanged("SignoutLabel");
        }

        public string SignoutLabel=> TeamExplorerConnection.ActiveConnection == null 
            ? "Sign out" 
            : string.Concat("Sign out ", TeamExplorerConnection.ActiveConnection.Account.DisplayName);

        private readonly CommandHandler _signoutCommand;
        public ICommand SignoutCommand => _signoutCommand;

        private readonly CommandHandler _cloneCommand;
        public ICommand CloneCommand => _cloneCommand;

        private readonly CommandHandler _createCommand;
        public ICommand CreateCommand => _createCommand;

        public ObservableCollection<ICodeCommitRepository> Repositories 
            => TeamExplorerConnection.ActiveConnection != null
                ? TeamExplorerConnection.ActiveConnection.Repositories
                : null;

        public ICodeCommitRepository SelectedRepository { get; set; }

        private void OnClone()
        {
            new CloneRepositoryController().Execute();
        }

        private void OnCreate()
        {
            new CreateRepositoryController().Execute();
        }

        private void OnSignout()
        {
            TeamExplorerConnection.ActiveConnection.Signout();
        }

        public void OpenRepository()
        {
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
        }
    }
}
