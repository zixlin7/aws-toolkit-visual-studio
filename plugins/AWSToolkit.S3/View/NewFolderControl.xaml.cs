using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;


namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for NewFolderControl.xaml
    /// </summary>
    public partial class NewFolderControl : BaseAWSControl
    {
        NewFolderController _controller;

        public NewFolderControl()
            : this(null)
        {
        }

        public NewFolderControl(NewFolderController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public NewFolderModel Model
        {
            get
            {
                return this._controller.Model;
            }
        }

        public override string Title
        {
            get
            {
                return "New Folder";
            }
        }

        public override bool OnCommit()
        {
            string newName = this.Model.NewFolderName == null ? string.Empty : this.Model.NewFolderName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("New name is required.");
                return false;
            }

            this._controller.Persist();
            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewFolderName.Focus();
        }
    }
}
