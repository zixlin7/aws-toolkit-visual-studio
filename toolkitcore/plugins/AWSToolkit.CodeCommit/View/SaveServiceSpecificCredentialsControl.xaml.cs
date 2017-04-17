using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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

            if(!string.IsNullOrEmpty(msg))
            {
                this._ctlMessage.Text = msg;
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
