using System.Windows;
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

        public CreateBucketModel Model => this._controller.Model;

        public override string Title => "Create Bucket";

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
