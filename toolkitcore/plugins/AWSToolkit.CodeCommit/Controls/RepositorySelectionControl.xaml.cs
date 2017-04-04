using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CodeCommit.Controls
{
    /// <summary>
    /// Interaction logic for RepositorySelectionControl.xaml
    /// </summary>
    public partial class RepositorySelectionControl : BaseAWSControl
    {
        public RepositorySelectionControl()
        {
            InitializeComponent();
        }

        public RepositorySelectionControl(RepositorySelectionController controller)
            : this()
        {
            Controller = controller;
            DataContext = Controller.Model;
        }

        public RepositorySelectionController Controller { get; }

        public override string Title => "Clone AWS CodeCommit Repository";

        public override bool Validated()
        {
            return Controller.Model.SelectedRepository != null && !string.IsNullOrEmpty(Controller.Model.LocalFolder);
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
