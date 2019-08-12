using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontStreamingDistributionViewModel : CloudFrontBaseDistributionViewModel, ICloudFrontStreamingDistributionViewModel
    {
        public CloudFrontStreamingDistributionViewModel(CloudFrontStreamingDistributeViewMetaNode metaNode, CloudFrontRootViewModel viewModel, StreamingDistribution distribution)
            : base(metaNode, viewModel, distribution.Id, GetName(distribution))
        {
            this.DomainName = distribution.DomainName;
            this.Aliases = distribution.StreamingDistributionConfig.Aliases;
        }

        public CloudFrontStreamingDistributionViewModel(CloudFrontStreamingDistributeViewMetaNode metaNode, CloudFrontRootViewModel viewModel, StreamingDistributionSummary distribution)
            : base(metaNode, viewModel, distribution.Id, GetName(distribution))
        {
            this.DomainName = distribution.DomainName;
            this.Aliases = distribution.Aliases;
        }

        protected override string IconName => "Amazon.AWSToolkit.CloudFront.Resources.EmbeddedImages.streaming-distribution.png";

        public static string GetName(StreamingDistribution distribution)
        {
            if (distribution.StreamingDistributionConfig.S3Origin.DomainName == null)
                return "No Origins";

            return distribution.StreamingDistributionConfig.S3Origin.DomainName;
        }

        public static string GetName(StreamingDistributionSummary distribution)
        {
            if (distribution.S3Origin.DomainName == null)
                return "No Origins";

            return distribution.S3Origin.DomainName;
        }

        protected override string FetchETag()
        {
            var response = CFClient.GetStreamingDistribution(new GetStreamingDistributionRequest() { Id = this.DistributionId });
            return response.ETag;
        }
    }
}
