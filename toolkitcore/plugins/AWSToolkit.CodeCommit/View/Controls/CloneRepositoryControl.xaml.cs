using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Model;

namespace Amazon.AWSToolkit.CodeCommit.View.Controls
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

            // Trap model property changes so we can forward them onto the
            // dialog host and enable the OK button dynamically (the host
            // will call our IsValidated handler)
            Controller.Model.PropertyChanged += ModelOnPropertyChanged;

            Loaded += (sender, e) => RefreshRepositoryList();
        }

        public CloneRepositoryController Controller { get; }

        public override string Title => "Clone AWS CodeCommit Repository";

        public override bool SupportsDynamicOKEnablement => true;

        private void RefreshRepositoryList()
        {
            // Skip loading while the form is being created
            if (IsLoaded)
            {
                Controller.Model.RefreshRepositoryList();
            }
        }

        public override bool Validated()
        {
            // deeper validation on the folder will be done (for now) when the user presses OK -
            // this just gets the OK button enabled.
            return Controller.Model.SelectedRepository != null && !string.IsNullOrEmpty(Controller.Model.SelectedFolder);
        }

        public override bool OnCommit()
        {
            var validationFailMsg = CloneRepositoryModel.IsFolderValidForRepo(Controller.Model.SelectedFolder);
            if (string.IsNullOrEmpty(validationFailMsg))
            {
                return true;
            }

            ToolkitFactory.Instance.ShellProvider.ShowError("Folder Error", $"The selected folder cannot be used to clone into. {validationFailMsg}");
            return false;
        }

        public void ShowQueryingStatus(bool queryActive)
        {
            _ctlRepositoryList.Opacity = queryActive ? .3 : 1;
            _ctlQueryingResourcesOverlay.Visibility = queryActive ? Visibility.Visible : Visibility.Hidden;
        }

        private void OnClickBrowseFolder(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog()
            {
                Description = "Select the folder to contain the repository",
                ShowNewFolderButton = true
            };

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Controller.Model.BaseFolder = dlg.SelectedPath;
            }
        }

        private void OnRepositorySelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Controller.Model.SelectedFolder = Controller.Model.SelectedRepository == null 
                ? Controller.Model.BaseFolder 
                : Path.Combine(Controller.Model.BaseFolder, Controller.Model.SelectedRepository.Name);
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // if this is the pseudo-property indicating the model is refreshing the repo
            // list, toggle the 'querying' spinner
            if (propertyChangedEventArgs.PropertyName.Equals(CloneRepositoryModel.RepoListRefreshStartingPropertyName))
            {
                ShowQueryingStatus(true);
            }

            if (propertyChangedEventArgs.PropertyName.Equals(CloneRepositoryModel.RepoListRefreshCompletedPropertyName))
            {
                ShowQueryingStatus(false);
            }

            // simply trigger our own notification so the dialog host will detect and call 
            // us back to validate and enable/disable the OK button
            NotifyPropertyChanged(propertyChangedEventArgs.PropertyName);
        }

        private void OnRegionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshRepositoryList();
        }

        private void OnSelectionChangedSortBy(object sender, SelectionChangedEventArgs e)
        {
            RefreshRepositoryList();
        }

        private void OnSelectionChangedOrder(object sender, SelectionChangedEventArgs e)
        {
            RefreshRepositoryList();
        }
    }
}
