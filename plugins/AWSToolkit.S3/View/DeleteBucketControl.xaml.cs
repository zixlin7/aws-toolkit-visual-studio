using Amazon.AWSToolkit.CommonUI;
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

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for DeleteBucketControl.xaml
    /// </summary>
    public partial class DeleteBucketControl : BaseAWSControl
    {
        /// <summary>
        /// Flag which indicates if the bucket should be deleted 
        /// along with the objects in it.
        /// </summary>
        public bool DeleteBucketWithObjects { get; set; }

        /// <summary>
        /// Name if the bucket to be deleted.
        /// </summary>
        public string BucketName { get; private set; }

        /// <summary>
        /// Title for the window in which the control is displayed.
        /// </summary>
        public override string Title
        {
            get
            {
                return "Delete Bucket";
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="bucketName">Name of the bucket to be deleted.</param>
        public DeleteBucketControl(string bucketName)
        {
            this.BucketName = bucketName;      
            this.DataContext = this;                  
            InitializeComponent();   
        }
    }
}
