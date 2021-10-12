using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.CloudFront.Model;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public class CloudFrontDistributionViewModel : CloudFrontBaseDistributionViewModel, ICloudFrontDistributionViewModel
    {
        public CloudFrontDistributionViewModel(CloudFrontDistributeViewMetaNode metaNode, CloudFrontRootViewModel viewModel, Distribution distribution)
            : base(metaNode, viewModel, distribution.Id, GetName(distribution))
        {
            this.DomainName = distribution.DomainName;
            this.Aliases = distribution.DistributionConfig.Aliases;
            this.Origins = distribution.DistributionConfig.Origins;
        }

        public CloudFrontDistributionViewModel(CloudFrontDistributeViewMetaNode metaNode, CloudFrontRootViewModel viewModel, DistributionSummary distribution)
            : base(metaNode, viewModel, distribution.Id, GetName(distribution))
        {
            this.DomainName = distribution.DomainName;
            this.Aliases = distribution.Aliases;
            this.Origins = distribution.Origins;
        }

        public Origins Origins
        {
            get;
        }

        protected override string IconName => AwsImageResourcePath.CloudFrontDownloadDistribution.Path;

        public static string GetName(Distribution distribution)
        {
            if (distribution.DistributionConfig.Origins.Items.Count == 0)
                return "No Origins";

            return distribution.DistributionConfig.Origins.Items[0].DomainName;
        }

        public static string GetName(DistributionSummary distribution)
        {
            if (distribution.Origins.Items.Count == 0)
                return "No Origins";

            return distribution.Origins.Items[0].DomainName;
        }

        protected override string FetchETag()
        {
            var response = CFClient.GetDistribution(new GetDistributionRequest() { Id = this.DistributionId });
            return response.ETag;
        }
    }
}
