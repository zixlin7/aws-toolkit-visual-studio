using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.View;
using Amazon.S3;
using Amazon.S3.IO;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class NewFolderController
    {
        private readonly IAmazonS3 _s3Client;

        public NewFolderController(IAmazonS3 s3Client, string bucketName, string parentPath)
            : this(s3Client, new NewFolderModel(bucketName, parentPath))
        {
        }

        public NewFolderController(IAmazonS3 s3Client, NewFolderModel model)
        {
            _s3Client = s3Client;
            Model = model;
        }

        public NewFolderModel Model { get; }

        public bool Execute()
        {
            return ToolkitFactory.Instance.ShellProvider.ShowModal(new NewFolderControl(this));
        }

        public void Persist()
        {
            S3Directory.CreateDirectory(
                _s3Client,
                Model.BucketName,
                S3Path.Combine(Model.ParentPath, S3Path.ToDirectory(Model.NewFolderName)));
        }
    }
}
