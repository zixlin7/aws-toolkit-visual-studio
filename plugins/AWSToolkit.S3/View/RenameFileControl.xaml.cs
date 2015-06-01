using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for RenameFileControl.xaml
    /// </summary>
    public partial class RenameFileControl : BaseAWSControl
    {
        RenameFileController _controller;
        RenameFileModel _model;

        public RenameFileControl()
            : this(null)
        {
        }

        public RenameFileControl(RenameFileController controller)
        {
            this._controller = controller;
            this._model = controller.Model;
            this.DataContext = this._model;

            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Rename";
            }
        }

        public override bool OnCommit()
        {
            string newName = this._model.NewFileName == null ? string.Empty : this._model.NewFileName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("New name is required.");
                return false;
            }

            if (!this._model.OrignalFullPathKey.Equals(this._model.NewFileName))
                this._controller.Persist();
            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewFileName.Focus();
        }
    }
}
