using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Amazon.AWSToolkit.CodeCommit.Controller;

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
        }

        public CreateRepositoryController Controller { get; }

        public override string Title => "Clone AWS CodeCommit Repository";

        public override bool Validated()
        {
            return !string.IsNullOrEmpty(Controller.Model.Name) && !string.IsNullOrEmpty(Controller.Model.LocalFolder);
        }

        public override bool OnCommit()
        {
            return true;
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
