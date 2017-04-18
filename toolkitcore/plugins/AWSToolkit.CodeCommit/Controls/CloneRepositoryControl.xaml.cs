using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CodeCommit.Controls
{
    /// <summary>
    /// Interaction logic for CloneRepositoryControl.xaml
    /// </summary>
    public partial class CloneRepositoryControl
    {
        public CloneRepositoryControl()
        {
            InitializeComponent();
        }

        public CloneRepositoryControl(CloneRepositoryController controller)
            : this()
        {
            Controller = controller;
            DataContext = Controller.Model;
        }

        public CloneRepositoryController Controller { get; }

        public override string Title
        {
            get { return "Clone AWS CodeCommit Repository"; }
        }

        public override bool Validated()
        {
            return Controller.Model.SelectedRepository != null && !string.IsNullOrEmpty(Controller.Model.SelectedFolder);
        }

        public override bool OnCommit()
        {
            return true;
        }

        public void ShowQueryingStatus(bool queryActive)
        {
            _ctlQueryingResourcesOverlay.Visibility = queryActive ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Controller.Model.RefreshRepositoryList();
        }

        private void OnClickBrowseFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog()
            {
                Description = @"Select the folder to contain the repository",
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Controller.Model.SelectedFolder = dlg.SelectedPath;
            }
        }
    }
}
