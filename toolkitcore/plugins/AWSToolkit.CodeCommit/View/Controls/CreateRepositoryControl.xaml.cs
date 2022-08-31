using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.Shared;
using Amazon.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.View.Controls
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
            Controller.Model.PropertyChanged += ModelOnPropertyChanged;
        }

        public CreateRepositoryController Controller { get; }

        public override string Title => "Create a New AWS CodeCommit Repository";

        public override bool SupportsDynamicOKEnablement => true;

        public override bool Validated()
        {
            // Deeper validation on the folder will be done (for now) when the user presses OK -
            // this just gets the OK button enabled.
            return !string.IsNullOrEmpty(Controller.Model.Name) && !string.IsNullOrEmpty(Controller.Model.SelectedFolder);
        }

        public override bool OnCommit()
        {
            var validationFailMsg = CloneRepositoryModel.IsFolderValidForRepo(Controller.Model.SelectedFolder);

            if (!string.IsNullOrEmpty(validationFailMsg))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Folder Error", $"The selected folder cannot be used to contain the new repository. {validationFailMsg}");
                return false;
            }

            try
            {
                var client = Controller.Model.GetClientForRegion(Controller.Model.SelectedRegion);
                var response = client.GetRepository(new GetRepositoryRequest {RepositoryName = Controller.Model.Name});
                if (response.RepositoryMetadata != null)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Repository Exists Error",
                        $"A repository with name {Controller.Model.Name} exists in the selected region.");
                    return false;
                }
            }
            catch (RepositoryDoesNotExistException) { }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error", "Unable to create a repository with the selected name due to error: " + e.Message);
                return false;
            }

            return true;
        }

        private void OnClickBrowseFolder(object sender, RoutedEventArgs e)
        {
            Controller.BrowseForBaseFolder();
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            NotifyPropertyChanged(propertyChangedEventArgs.PropertyName);
        }

        private void OnGitIgnoreSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Controller.Model.SelectedGitIgnore == null)
            {
                return;
            }

            var selection = Controller.Model.SelectedGitIgnore;
            if (selection.GitIgnoreType == GitIgnoreOption.OptionType.Custom)
            {
                var dlg = new OpenFileDialog
                {
                    CheckPathExists = true,
                    CheckFileExists = true,
                    Filter = ".gitignore files (.gitignore)|.gitignore"
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    selection.CustomFilename = Path.GetFullPath(dlg.FileName);
                    selection.DisplayText = $"Custom [{dlg.FileName}]";
                }
            }
        }
    }
}
