using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CodeCommit.View
{
    /// <summary>
    /// Interaction logic for CreateRepositoryControl.xaml
    /// </summary>
    public partial class CreateRepositoryControl 
    {
        public CreateRepositoryControl()
        {
            InitializeComponent();
        }

        public CreateRepositoryControl(CreateRepositoryController controller)
            : this()
        {
            Controller = controller;
            DataContext = Controller.Model;
            Controller.Model.PropertyChanged += (sender, args) =>
            {
                SetOkButtonEnablement(Validated());
            };
        }

        public CreateRepositoryController Controller { get; }

        public override string Title => "Create a New AWS CodeCommit Repository";

        public override bool Validated()
        {
            return !string.IsNullOrEmpty(Controller.Model.Name) && !string.IsNullOrEmpty(Controller.Model.LocalFolder);
        }

        public override bool OnCommit()
        {
            return true;
        }

        private OkCancelDialogHost _host;
        private OkCancelDialogHost Host
        {
            get
            {
                if (_host == null)
                {
                    _host = FindHost<OkCancelDialogHost>();
                }

                return _host;
            }
        }

        public void SetOkButtonEnablement(bool okEnabled)
        {
            var host = Host;
            if (host != null)
                host.IsOkEnabled = okEnabled;
        }

        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void OnClickBrowseFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog()
            {
                Description = @"Select the folder to clone into",
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var repositoryFolder = dlg.SelectedPath;
                //if (Directory.Exists(repositoryFolder))
                //{
                //    // if files only exist in subfolders, then by definition at least one top level folder
                //    // must exist - saves us having to scan all subfolders for files.
                //    var files = Directory.GetFiles(repositoryFolder, "*.*", SearchOption.TopDirectoryOnly);
                //    var subFolders = Directory.GetDirectories(repositoryFolder, "*.*", SearchOption.TopDirectoryOnly);
                //    if (files.Any() || subFolders.Any())
                //    {
                //        ToolkitFactory.Instance.ShellProvider.ShowError("Non-empty Folder", "The selected folder is not empty. Please choose another location.");
                //        return;
                //    }
                //}

                Controller.Model.LocalFolder = repositoryFolder;
            }
        }


    }
}
