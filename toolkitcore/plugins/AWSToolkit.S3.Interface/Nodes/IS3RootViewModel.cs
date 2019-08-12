using Amazon.S3;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public interface IS3RootViewModel : IServiceRootViewModel
    {
        IAmazonS3 S3Client { get; }

        void AddBucket(string bucketName);
        void RemoveBucket(string bucketName);
    }
}
