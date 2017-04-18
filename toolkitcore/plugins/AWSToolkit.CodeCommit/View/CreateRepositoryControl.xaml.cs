using System.ComponentModel;
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
            Controller.Model.PropertyChanged += ModelOnPropertyChanged;
        }

        public CreateRepositoryController Controller { get; }

        public override string Title
        {
            get { return "Create a New AWS CodeCommit Repository"; }
        }

        public override bool Validated()
        {
            return !string.IsNullOrEmpty(Controller.Model.Name) && !string.IsNullOrEmpty(Controller.Model.LocalFolder);
        }

        public override bool OnCommit()
        {
            return true;
        }

        public override bool SupportsDynamicOKEnablement
        {
            get { return true; }
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
                Controller.Model.LocalFolder = dlg.SelectedPath;
            }
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            NotifyPropertyChanged(propertyChangedEventArgs.PropertyName);
        }


    }
}
