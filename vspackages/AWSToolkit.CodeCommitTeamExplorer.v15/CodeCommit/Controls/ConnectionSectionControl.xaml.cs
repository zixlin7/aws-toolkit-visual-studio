using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CredentialManagement;
using log4net;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controls
{
    /// <summary>
    /// This is the panel that shows in Team Explorer when users connect
    /// to CodeCommit with credentials. It offers users a way to clone/create
    /// CodeCommit repos, and shows repos that have been previously retrieved locally.
    /// </summary>
    public partial class ConnectionSectionControl
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ConnectionSectionControl));

        public ConnectionSectionControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        public ConnectSectionViewModel ViewModel => DataContext as ConnectSectionViewModel;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // If the Toolkit Package has not loaded up yet, we won't have access to
            // the UI Styles. Load those in when the toolkit is available.
            ToolkitFactory.AddToolkitInitializedDelegate(() =>
            {
                Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ThemeUtil.UpdateDictionariesForTheme(this.Resources);
                });
            });
        }

        private void OnRepositoryMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.OpenRepository();
        }

        private void OnRepositoryMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (ViewModel?.SelectedRepository == null)
                return;

            var menu = new ContextMenu();

            var style = FindResource("awsContextMenuStyle") as Style;
            if (style != null)
                menu.Style = style;

            var browseInConsole = new MenuItem
            {
                Header = "Browse in Console"
                // todo: Icon = IconHelper.GetIcon(this.GetType().Assembly, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.detach-volume.png");
            };
            browseInConsole.Click += OnClickBrowseRepositoryMenuItem;

            menu.Items.Add(browseInConsole);

            menu.Items.Add(new Separator());
            var updateCredentials = new MenuItem
            {
                Header = "Update Git Credentials"
            };
            updateCredentials.Click += OnClickUpdateCredentials;
            menu.Items.Add(updateCredentials);

            menu.PlacementTarget = this;

            _ctlRepositoriesList.ContextMenu = menu;
        }

        private void OnClickBrowseRepositoryMenuItem(object sender, RoutedEventArgs e)
        {
            if (TeamExplorerConnection.CodeCommitPlugin == null)
                return;

            try
            {
                var url = TeamExplorerConnection.CodeCommitPlugin.GetConsoleBrowsingUrl(ViewModel.SelectedRepository.LocalFolder);
                ToolkitFactory.Instance.ShellProvider.OpenInBrowser(url, false);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(ConnectionSectionControl)).Error("Error browsing Git repositories", ex);
            }
        }

        // we've found a repo on disk that does not have service credentials available, likely
        // as a result of being cloned outside of VS, or in Team Explorer, before we had our
        // integration. This option allows the user to set up their git credentials ready for 
        // any subsequent use.
        private void OnClickUpdateCredentials(object sender, RoutedEventArgs routedEventArgs)
        {
            if (TeamExplorerConnection.CodeCommitPlugin == null)
            {
                LOGGER.Warn("Skipping OnClickUpdateCredentials because the main toolkit instance hasn't initialized yet.");
                return;
            }

            try
            {
                var region = TeamExplorerConnection.CodeCommitPlugin.GetRepositoryRegion(ViewModel.SelectedRepository.LocalFolder);
                TeamExplorerConnection.CodeCommitPlugin.ObtainGitCredentials(ViewModel.Account, region, true);
            }
            catch(Exception ex)
            {
                LogManager.GetLogger(typeof(ConnectionSectionControl)).Error("Error updating Git credentials", ex);
            }
        }
    }
}
