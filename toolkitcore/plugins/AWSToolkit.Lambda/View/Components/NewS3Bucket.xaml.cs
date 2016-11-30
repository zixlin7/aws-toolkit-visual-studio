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

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for NewS3Bucket.xaml
    /// </summary>
    public partial class NewS3Bucket : BaseAWSControl
    {
        private IAmazonS3 S3Client { get; }
        public NewS3Bucket()
        {
            InitializeComponent();
            DataContext = this;
        }

        public NewS3Bucket(IAmazonS3 s3Client )
            : this()
        {
            this.S3Client = s3Client;
        }

        public override string Title
        {
            get
            {
                return "Create Bucket";
            }
        }

        public string BucketName
        {
            get;
            set;
        }

        public override bool OnCommit()
        {
            try
            {
                this.S3Client.PutBucket(this.BucketName);
                return true;
            }
            catch(Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error", "Error creating S3 Bucket: " + e.Message);
                return false;
            }
        }

        public override bool Validated()
        {
            if(string.IsNullOrEmpty(this.BucketName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Bucket name is required");
                return false;
            }

            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewBucketName.Focus();
        }
    }
}
