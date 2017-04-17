using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Model;
using Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CodeCommit.Controls
{
    /// <summary>
    /// Interaction logic for ConnectionSectionControl.xaml
    /// </summary>
    public partial class ConnectionSectionControl
    {
        public ConnectionSectionControl()
        {
            InitializeComponent();
        }

        public ConnectSectionViewModel ViewModel => DataContext as ConnectSectionViewModel;

        private void OnRepositoryMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.OpenRepository();
        }

        private void OnClickBrowseRepositoryMenuItem(object sender, RoutedEventArgs e)
        {
            var url = TeamExplorerConnection.CodeCommitPlugin.GetConsoleBrowsingUrl(ViewModel.SelectedRepository.LocalFolder);
            ToolkitFactory.Instance.ShellProvider.OpenInBrowser(url, false);
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
            menu.PlacementTarget = this;

            _ctlRepositoriesList.ContextMenu = menu;
        }
    }
}
