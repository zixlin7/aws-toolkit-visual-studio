using System.Windows;

using Amazon.AWSToolkit.CodeCommit.Controller;

using Microsoft.Win32;

namespace Amazon.AWSToolkit.CodeCommit.View
{
    /// <summary>
    /// Interaction logic for SaveServiceSpecificCredentialsControl.xaml
    /// </summary>
    public partial class SaveServiceSpecificCredentialsControl
    {
        public SaveServiceSpecificCredentialsControl()
        {
            InitializeComponent();
        }

        public SaveServiceSpecificCredentialsControl(SaveServiceSpecificCredentialsController controller, string msg = null)
            : this()
        {
            Controller = controller;
            DataContext = controller.Model;
            Controller.Model.PropertyChanged += (sender, e) => NotifyPropertyChanged(e.PropertyName);

            if (!string.IsNullOrEmpty(msg))
            {
                _ctlMessage.Text = msg;
            }
        }

        public SaveServiceSpecificCredentialsController Controller { get; }

        public override string Title => Controller?.Model == null ? null : "Save Generated Credentials";

        public override bool Validated()
        {
            return !string.IsNullOrEmpty(Controller?.Model.Filename);
        }

        public override bool OnCommit()
        {
            return Controller.Model.SaveToFile();
        }

        public override bool SupportsDynamicOKEnablement => true;

        private void OnClickBrowseForFile(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save Generated Credentials to File",
                Filter = "CSV Files|*.csv|All Files|*.*",
                OverwritePrompt = true
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                Controller.Model.Filename = dlg.FileName;
            }
        }
    }
}
