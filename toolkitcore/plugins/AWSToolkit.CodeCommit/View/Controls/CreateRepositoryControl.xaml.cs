using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Model;

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

        public override string Title
        {
            get { return "Create a New AWS CodeCommit Repository"; }
        }

        public override bool SupportsDynamicOKEnablement
        {
            get { return true; }
        }

        public override bool Validated()
        {
            // deeper validation on the folder will be done (for now) when the user presses OK -
            // this just gets the OK button enabled.
            return !string.IsNullOrEmpty(Controller.Model.Name) && !string.IsNullOrEmpty(Controller.Model.SelectedFolder);
        }

        public override bool OnCommit()
        {
            var validationFailMsg = CloneRepositoryModel.IsFolderValidForRepo(Controller.Model.SelectedFolder);
            if (string.IsNullOrEmpty(validationFailMsg))
                return true;

            ToolkitFactory.Instance.ShellProvider.ShowError("Folder Error", "The selected folder cannot be used to contain the new repository. " + validationFailMsg);
            return false;
        }

        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // todo: find repo names in region and validate
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
                Controller.Model.BaseFolder = dlg.SelectedPath;
            }
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            NotifyPropertyChanged(propertyChangedEventArgs.PropertyName);
        }


    }
}
