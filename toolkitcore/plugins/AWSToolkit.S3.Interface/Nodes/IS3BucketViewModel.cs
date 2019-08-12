using Amazon.S3;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public interface IS3BucketViewModel : IViewModel
    {
        IAmazonS3 S3Client { get; }
    }
}
