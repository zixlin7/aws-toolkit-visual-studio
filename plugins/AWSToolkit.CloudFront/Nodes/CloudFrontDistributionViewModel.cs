using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;

using Amazon.AWSToolkit.Navigator.Node;

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
            private set;
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.CloudFront.Resources.EmbeddedImages.distribution.png";
            }
        }

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
