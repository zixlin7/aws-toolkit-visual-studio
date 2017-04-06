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
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;
using log4net;
using Microsoft.Win32;

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
            var selectedRepository = codeCommitPlugin.SelectRepositoryToClone(account, region, GetLocalClonePathFromGitProvider());
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
            gitServices?.Clone(selectedRepository.RepositoryUrl, selectedRepository.LocalFolder, svcCredentials);
       }

        private void OnCreate()
        {
        }

        private void OnDisconnect()
        {
            var currentConnection = ConnectionsManager.Instance.TeamExplorerAccount;
            ConnectionsManager.Instance.DeregisterProfileConnection(currentConnection);
        }

        // The Default Repository Path that VS uses is hidden in an internal
        // service 'ISccSettingsService' registered in an internal service
        // 'ISccServiceHost' in an assembly with no public types that's
        // always loaded with VS if the git service provider is loaded
        public string GetLocalClonePathFromGitProvider()
        {
            var clonePath = string.Empty;

            try
            {
#if VS2017_OR_LATER
                const string TEGitKey = @"SOFTWARE\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";
#else
                const string TEGitKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\TeamFoundation\GitSourceControl";
#endif

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(TEGitKey + "\\General", true))
                {
                    clonePath = (string)key?.GetValue("DefaultRepositoryPath", string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
                }
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Error loading the default cloning path from the registry '{0}'", e);
            }

            if (string.IsNullOrEmpty(clonePath))
            {
                clonePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Source", "Repos");
            }

            return clonePath;
        }

    }
}
