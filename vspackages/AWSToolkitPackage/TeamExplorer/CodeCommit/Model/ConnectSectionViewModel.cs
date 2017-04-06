using System;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CodeCommit.Interface;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
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
            var codeCommitPlugin = ToolkitFactory.Instance.QueryPluginService(typeof(IAWSCodeCommit)) as IAWSCodeCommit;
            if (codeCommitPlugin == null)
            {
                LOGGER.Error("Called to clone repository but CodeCommit plugin not loaded");
                return;
            }

            // by now we'll have made sure the user has a profile set up, so default account and region
            // bindings are always available
            var account = ConnectionsManager.Instance.TeamExplorerAccount;
            var region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;
            var selectedRepository = codeCommitPlugin.SelectRepositoryToClone(account, region, null);
            if (selectedRepository == null)
                return;

            // if the account doesn't have service-specific credentials for CodeCommit, now would be
            // a good time to get and store them
            var svcCredentials
                = ServiceSpecificCredentialStoreManager
                    .Instance
                    .GetCredentialsForService(account.SettingsUniqueKey, CodeCommitConstants.CodeCommitServiceCredentialsName);
            if (svcCredentials == null)
            {
                var registerCredentialsController = new RegisterServiceCredentialsController(account);
                if (!registerCredentialsController.Execute().Success)
                    return;

                svcCredentials = registerCredentialsController.Credentials;
            }

            var repoUrl = selectedRepository.RepositoryUrl.TrimEnd('/');

            // experiment: push the service specific credentials to the Windows credential store
            var uri = new Uri(repoUrl);
            var credentialKey = string.Format("git:{0}://{1}", uri.Scheme, uri.DnsSafeHost);
            var gitCredentials 
                = new GitCredentials(svcCredentials.Username, svcCredentials.Password, credentialKey);

            gitCredentials.Save();

            // delegate the actual clone operation to either Team Explorer or the CodeCommit plugin
            var gitServices = ToolkitFactory
                                .Instance
                                .ShellProvider
                                .QueryShellProviderService<IAWSToolkitGitServices>();
            if (gitServices == null)
            {
                gitServices = ToolkitFactory
                                .Instance
                                .QueryPluginService(typeof(IAWSToolkitGitServices)) as IAWSToolkitGitServices;
            }
            gitServices?.Clone(repoUrl, selectedRepository.LocalFolder, account);
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
