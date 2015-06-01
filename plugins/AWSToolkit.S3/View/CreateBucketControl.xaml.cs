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
    /// Interaction logic for CreateBucketControl.xaml
    /// </summary>
    public partial class CreateBucketControl : BaseAWSControl
    {
        CreateBucketController _controller;

        public CreateBucketControl()
            : this(null)
        {
        }

        public CreateBucketControl(CreateBucketController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public CreateBucketModel Model
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
                return "Create Bucket";
            }
        }

        public override bool OnCommit()
        {
            string newName = this._controller.Model.BucketName == null ? string.Empty : this._controller.Model.BucketName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is required.");
                return false;
            }

            return this._controller.Persist();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewBucketName.Focus();
        }
    }
}
