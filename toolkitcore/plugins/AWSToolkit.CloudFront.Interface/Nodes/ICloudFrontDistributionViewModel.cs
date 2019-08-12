using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public interface ICloudFrontDistributionViewModel : ICloudFrontBaseDistributionViewModel
    {
        Origins Origins { get; }
    }
}
