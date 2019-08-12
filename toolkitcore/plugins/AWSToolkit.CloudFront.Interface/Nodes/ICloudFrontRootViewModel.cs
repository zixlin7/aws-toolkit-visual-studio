using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public interface ICloudFrontRootViewModel : IServiceRootViewModel
    {
        IAmazonCloudFront CFClient { get; }

        ICloudFrontBaseDistributionViewModel AddDistribution(Distribution distribution);
        ICloudFrontBaseDistributionViewModel AddDistribution(StreamingDistribution streamingDistribution);

        void RemoveDistribution(string name);
    }
}
