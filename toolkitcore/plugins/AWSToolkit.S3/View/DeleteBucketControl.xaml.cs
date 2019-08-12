using Amazon.AWSToolkit.CommonUI;

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
        public string BucketName { get; }

        /// <summary>
        /// Title for the window in which the control is displayed.
        /// </summary>
        public override string Title => "Delete Bucket";

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
