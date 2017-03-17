using System;
using System.ComponentModel;
using System.Windows.Input;
using Amazon.AWSToolkit.Account;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.CommonUI;

using log4net;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model
{
    public class ConnectSectionViewModel : TeamExplorerSectionViewModelBase
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(ConnectSectionViewModel));

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

        public string DisconnectLabel
        {
            get
            {
                return ConnectionsManager.Instance.TeamExplorerAccount == null 
                    ? "Disconnect" 
                    : string.Concat("Disconnect ", ConnectionsManager.Instance.TeamExplorerAccount.DisplayName);
            }
        }

        private readonly CommandHandler _disconnectCommand;
        public ICommand DisconnectCommand
        {
            get { return _disconnectCommand; }
        }

        private readonly CommandHandler _cloneCommand;
        public ICommand CloneCommand
        {
            get { return _cloneCommand; }
        }

        private readonly CommandHandler _createCommand;
        public ICommand CreateCommand
        {
            get { return _createCommand; }
        }

        private void OnClone()
        {
            var codeCommitPlugin 
                = ToolkitFactory.Instance.ShellProvider.QueryAWSToolkitPluginService(typeof(IAWSCodeCommit))
                                as IAWSCodeCommit;

            try
            {
                if (codeCommitPlugin.CloneRepository(ConnectionsManager.Instance.TeamExplorerAccount))
                {
                    
                }
                else
                {
                    
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Exception during repository selection/clone", e);
            }
        }

        private void OnCreate()
        {
        }

        private void OnDisconnect()
        {
            var currentConnection = ConnectionsManager.Instance.TeamExplorerAccount;
            ConnectionsManager.Instance.DeregisterProfileConnection(currentConnection);
        }

    }
}
