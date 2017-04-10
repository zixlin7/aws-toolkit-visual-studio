using System;
using System.IO;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
using log4net;
using Microsoft.Win32;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model
{
    public class ConnectSectionViewModel : TeamExplorerSectionViewModelBase
    {
        public ConnectSectionViewModel()
        {
            ConnectionsManager.Instance.CollectionChanged += ConnectionsCollectionChanged;
            ConnectionsManager.Instance.OnTeamExplorerBindingChanged += OnTeamExplorerBindingChanged;

            _disconnectCommand = new CommandHandler(OnDisconnect, true);
            _cloneCommand = new CommandHandler(OnClone, true);
            _createCommand = new CommandHandler(OnCreate, true);
        }

        // triggers a refresh of the repos collection in the panel
        private void ConnectionsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshRepositoriesList();
        }

        // used to trigger notification that the Disconnect button label should update
        private void OnTeamExplorerBindingChanged(AccountViewModel boundAccount)
        {
            RaisePropertyChanged("DisconnectLabel");
        }

        public void RefreshRepositoriesList()
        {
        }

        public string DisconnectLabel => ConnectionsManager.Instance.TeamExplorerAccount == null 
            ? "Disconnect" 
            : string.Concat("Disconnect ", ConnectionsManager.Instance.TeamExplorerAccount.DisplayName);

        private readonly CommandHandler _disconnectCommand;
        public ICommand DisconnectCommand => _disconnectCommand;

        private readonly CommandHandler _cloneCommand;
        public ICommand CloneCommand => _cloneCommand;

        private readonly CommandHandler _createCommand;
        public ICommand CreateCommand => _createCommand;

        private void OnClone()
        {
            new CloneController().Execute();
        }

        private void OnCreate()
        {
        }

        private void OnDisconnect()
        {
            var currentConnection = ConnectionsManager.Instance.TeamExplorerAccount;
            ConnectionsManager.Instance.DeregisterProfileConnection(currentConnection);

            // remove all credentials we have placed into the OS credential store -
            // this follows the behavior of the GitHub plugin.
            PersistedCredentials.ClearAllPersistedTargets();
        }

    }
}
