using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

using Amazon.AWSToolkit.CodeCommit.Controller;
using Amazon.AWSToolkit.CodeCommit.Model;
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

            // trap model property changes so we can forward them onto the
            // dialog host and enable the OK button dynamically (the host
            // will call our IsValidated handler)
            Controller.Model.PropertyChanged += ModelOnPropertyChanged;
        }

        public CloneRepositoryController Controller { get; }

        public override string Title
        {
            get { return "Clone AWS CodeCommit Repository"; }
        }

        public override bool SupportsDynamicOKEnablement
        {
            get { return true; }
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
                return true;

            ToolkitFactory.Instance.ShellProvider.ShowError("Folder Error", "The selected folder cannot be used to clone into. " + validationFailMsg);
            return false;
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
            // simply trigger our own notification so the dialog host will detect and call 
            // us back to validate and enable/disable the OK button
            NotifyPropertyChanged(propertyChangedEventArgs.PropertyName);
        }


    }
}
